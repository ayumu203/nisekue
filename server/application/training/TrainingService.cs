using server.application.battle;
using server.application.player;
using server.domain.battle;
using server.domain.move;
using server.domain.player;
using server.domain.training;
using server.shared.constants.training;

namespace server.application.training;

public class TrainingService(
    IPlayerRepository playerRepository,
    ITrainingEnemyRepository trainingEnemyRepository,
    IMoveRepository moveRepository,
    IJobProfileRepository jobProfileRepository,
    IJobMoveLearningRuleRepository jobMoveLearningRuleRepository,
    PlayerJobService playerJobService,
    BattleService battleService,
    TrainingBattleFactory trainingBattleFactory,
    TrainingOutcomeJudge trainingOutcomeJudge,
    TrainingExpCalculator trainingExpCalculator)
{
    public async Task<TrainingEnemyView[]> GetTrainingEnemies(PlayerId playerId)
    {
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        var enemies = await trainingEnemyRepository.GetTrainingEnemiesAsync();
        var sortedEnemies = enemies
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Id.Value)
            .ToArray();
        var visibleEnemyLevelCap = ResolveVisibleEnemyLevelCap(player.JobLevel, sortedEnemies.Select(x => x.Level).ToArray());

        return sortedEnemies
            .Where(x => x.Level <= visibleEnemyLevelCap)
            .Select(x => new TrainingEnemyView(
                Id: x.Id.Value,
                Name: x.Name,
                ImagePath: x.ImagePath,
                Level: x.Level))
            .ToArray();
    }

    public async Task<TrainingResultView> ExecuteTraining(PlayerId playerId, TrainingEnemyId enemyId, IReadOnlyList<int?> playerMoveIds)
    {
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        var enemy = await trainingEnemyRepository.GetTrainingEnemyAsync(enemyId)
            ?? throw new KeyNotFoundException("敵が見つかりません。");

        var playerMoves = await LoadTrainingMovesAsync(player, playerMoveIds);

        var nowUtc = DateTimeOffset.UtcNow;
        var cooldownUntil = await playerRepository.TryStartTrainingCooldownAsync(
            playerId,
            nowUtc,
            TimeSpan.FromSeconds(TrainingConstants.Battle.CooldownSeconds));
        if (cooldownUntil is not null && cooldownUntil.Value > nowUtc)
        {
            throw new TrainingCooldownException(cooldownUntil.Value);
        }

        var actors = new[]
        {
            trainingBattleFactory.CreatePlayerActor(player, playerMoves),
            trainingBattleFactory.CreateEnemyActor(enemy)
        };
        var moves = trainingBattleFactory.CreateTrainingMoves(enemy, playerMoves);

        var (summary, metrics) = ResolveBattleUntilFinished(actors, playerMoves, moves);

        var exp = trainingExpCalculator.Calculate(player, enemy, metrics, summary.Outcome);
        var levelUpResult = ApplyExp(player, exp);

        await playerRepository.SaveAsync(player);

        return new TrainingResultView(
            TrainingResult: summary.Outcome.ToString(),
            Turn: summary.Turn,
            CurrentPlayerHp: summary.CurrentPlayerHp,
            MaxPlayerHp: player.Status.MaxHp,
            CurrentEnemyHp: summary.CurrentEnemyHp,
            MaxEnemyHp: enemy.Status.MaxHp,
            Exp: exp,
            IsLevelUp: levelUpResult.HasLeveledUp,
            NewlyLearnedMoves: await playerJobService.BuildLearnedMoveViewsAsync(levelUpResult.NewlyLearnedMoveIds));
    }

    private static int ResolveVisibleEnemyLevelCap(int playerLevel, IReadOnlyList<int> enemyLevels)
    {
        if (enemyLevels.Count == 0)
        {
            return 0;
        }

        foreach (var enemyLevel in enemyLevels)
        {
            if (enemyLevel > playerLevel)
            {
                return enemyLevel;
            }
        }

        return enemyLevels[^1];
    }

    private (TrainingBattleSummary Summary, TrainingContributionMetrics Metrics) ResolveBattleUntilFinished(
        IReadOnlyList<BattleActorInput> actors,
        IReadOnlyList<Move> playerMoves,
        IReadOnlyList<Move> moves)
    {
        ArgumentNullException.ThrowIfNull(actors);
        ArgumentNullException.ThrowIfNull(playerMoves);
        ArgumentNullException.ThrowIfNull(moves);

        var currentActors = actors.ToArray();
        var turn = 0;
        var playerDealtTotalDamage = 0;
        var playerEffectiveHealTotal = 0;

        while (turn < TrainingConstants.Battle.MaxTurns && turn < playerMoves.Count)
        {
            var playerActor = currentActors.Single(x => x.ActorId == trainingBattleFactory.PlayerActorId);
            var enemyActor = currentActors.Single(x => x.ActorId == trainingBattleFactory.EnemyActorId);
            if (playerActor.CurrentHp <= 0 || enemyActor.CurrentHp <= 0)
            {
                break;
            }

            var actions = trainingBattleFactory.CreateTurnActions(playerActor, enemyActor.ActorId, playerMoves[turn]);
            var resolution = battleService.ResolveTurn(new BattleTurnRequest(currentActors, actions, moves));
            playerDealtTotalDamage += resolution.ActionResults
                .Where(x => x.ActorId.Value == trainingBattleFactory.PlayerActorId)
                .SelectMany(x => x.TargetResults)
                .Where(x => x.TargetActorId.Value == trainingBattleFactory.EnemyActorId)
                .Sum(x => x.Damage);
            playerEffectiveHealTotal += resolution.ActionResults
                .Where(x => x.ActorId.Value == trainingBattleFactory.PlayerActorId)
                .SelectMany(x => x.TargetResults)
                .Where(x => x.TargetActorId.Value == trainingBattleFactory.PlayerActorId)
                .Sum(x => Math.Max(0, x.HpChange));
            turn++;
            currentActors = BuildNextTurnActors(currentActors, resolution.UpdatedStates);
        }

        var summary = BuildSummary(currentActors, turn);
        var metrics = BuildContributionMetrics(summary, playerDealtTotalDamage, playerEffectiveHealTotal);
        return (summary, metrics);
    }

    private BattleActorInput[] BuildNextTurnActors(
        IReadOnlyList<BattleActorInput> actors,
        IReadOnlyList<BattleActorState> states)
    {
        var stateMap = states.ToDictionary(x => x.Id.Value);
        return actors
            .Select(actor =>
            {
                var state = stateMap[actor.ActorId];
                return actor with
                {
                    CurrentHp = state.CurrentHp,
                    CurrentMp = state.CurrentMp,
                    Ailments = state.Ailments.ToArray(),
                    Buffs = state.Buffs.ToArray()
                };
            })
            .ToArray();
    }

    private TrainingBattleSummary BuildSummary(
        IReadOnlyList<BattleActorInput> actors,
        int turn)
    {
        var playerActor = actors.Single(x => x.ActorId == trainingBattleFactory.PlayerActorId);
        var enemyActor = actors.Single(x => x.ActorId == trainingBattleFactory.EnemyActorId);
        var outcome = trainingOutcomeJudge.Judge(
            [
                new BattleActorState(new BattleActorId(playerActor.ActorId), playerActor.CurrentHp ?? playerActor.BaseStatus.MaxHp, playerActor.CurrentMp ?? playerActor.BaseStatus.MaxMp),
                new BattleActorState(new BattleActorId(enemyActor.ActorId), enemyActor.CurrentHp ?? enemyActor.BaseStatus.MaxHp, enemyActor.CurrentMp ?? enemyActor.BaseStatus.MaxMp)
            ],
            new BattleActorId(trainingBattleFactory.PlayerActorId),
            new BattleActorId(trainingBattleFactory.EnemyActorId));

        return new TrainingBattleSummary(
            Turn: turn,
            CurrentPlayerHp: playerActor.CurrentHp ?? playerActor.BaseStatus.MaxHp,
            CurrentEnemyHp: enemyActor.CurrentHp ?? enemyActor.BaseStatus.MaxHp,
            Outcome: outcome);
    }

    private static TrainingContributionMetrics BuildContributionMetrics(
        TrainingBattleSummary summary,
        int playerDealtTotalDamage,
        int playerEffectiveHealTotal)
    {
        return new TrainingContributionMetrics(
            PlayerDealtTotalDamage: playerDealtTotalDamage,
            PlayerEffectiveHealTotal: playerEffectiveHealTotal,
            CurrentPlayerHp: summary.CurrentPlayerHp);
    }

    private LevelUpResult ApplyExp(Player player, int exp)
    {
        player.GainExp(exp);
        var jobProfile = jobProfileRepository.GetByJob(player.Job);
        var learningRule = jobMoveLearningRuleRepository.GetByJob(player.Job);
        return player.LevelUp(jobProfile, learningRule);
    }

    private async Task<Move[]> LoadTrainingMovesAsync(Player player, IReadOnlyList<int?> playerMoveIds)
    {
        ArgumentNullException.ThrowIfNull(playerMoveIds);

        if (playerMoveIds.Count != TrainingConstants.Battle.MaxTurns)
        {
            throw new ArgumentException($"特訓で指定できる技は{TrainingConstants.Battle.MaxTurns}ターン分固定です。", nameof(playerMoveIds));
        }

        var learnedMoveIds = player.MoveSet.GetLearnedMoveIds()
            .Select(x => x.Id)
            .ToHashSet();

        var moves = new List<Move>(playerMoveIds.Count);
        foreach (var moveId in playerMoveIds)
        {
            if (moveId is null)
            {
                moves.Add(trainingBattleFactory.CreatePlayerTrainingNormalAttack(player.Status));
                continue;
            }

            if (!learnedMoveIds.Contains(moveId.Value))
            {
                throw new ArgumentException($"未習得の技は指定できません。 moveId={moveId}", nameof(playerMoveIds));
            }

            var move = await moveRepository.GetMoveAsync(new MoveId(moveId.Value));
            if (move is null)
            {
                throw new ArgumentException($"存在しない技は指定できません。 moveId={moveId}", nameof(playerMoveIds));
            }

            moves.Add(move);
        }

        return moves.ToArray();
    }
}
