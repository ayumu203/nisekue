using server.domain.player;
using server.domain.training;
using server.shared.constants.training;

namespace server.application.training;

public class TrainingService(IPlayerRepository playerRepository, ITrainingEnemyRepository trainingEnemyRepository)
{
    public async Task<TrainingEnemyView[]> GetTrainingEnemies()
    {
        var enemies = await trainingEnemyRepository.GetTrainingEnemiesAsync();

        return enemies
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Id.Value)
            .Select(x => new TrainingEnemyView(
                Id: x.Id.Value,
                Name: x.Name,
                ImagePath: x.ImagePath,
                Level: x.Level))
            .ToArray();
    }

    public async Task<TrainingResultView> ExecuteTraining(PlayerId playerId, TrainingEnemyId enemyId)
    {
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        var enemy = await trainingEnemyRepository.GetTrainingEnemyAsync(enemyId)
            ?? throw new KeyNotFoundException("敵が見つかりません。");

        var nowUtc = DateTimeOffset.UtcNow;
        var cooldownUntil = await playerRepository.TryStartTrainingCooldownAsync(
            playerId,
            nowUtc,
            TimeSpan.FromSeconds(TrainingConstants.Battle.CooldownSeconds));
        if (cooldownUntil is not null && cooldownUntil.Value > nowUtc)
        {
            throw new TrainingCooldownException(cooldownUntil.Value);
        }

        var turnResult = new TurnResult(
            Turn: 0,
            CurrentPlayerHp: player.Status.MaxHp,
            CurrentEnemyHp: enemy.Status.MaxHp);

        for (var i = 0; i < TrainingConstants.Battle.MaxTurns; i++)
        {
            if (turnResult.CurrentPlayerHp <= 0 || turnResult.CurrentEnemyHp <= 0)
            {
                continue;
            }

            turnResult = ExecuteTurn(player, enemy, turnResult);
        }

        var trainingResult = "Draw";
        if (turnResult.CurrentPlayerHp > 0 && turnResult.CurrentEnemyHp <= 0)
        {
            trainingResult = "Win";
        }
        else if (turnResult.CurrentPlayerHp <= 0 && turnResult.CurrentEnemyHp > 0)
        {
            trainingResult = "Lose";
        }

        var exp = CalcExp(player, enemy, turnResult);
        var isLevelUp = ApplyExp(player, exp);

        await playerRepository.SaveAsync(player);

        return new TrainingResultView(
            TrainingResult: trainingResult,
            Turn: turnResult.Turn,
            CurrentPlayerHp: turnResult.CurrentPlayerHp,
            MaxPlayerHp: player.Status.MaxHp,
            CurrentEnemyHp: turnResult.CurrentEnemyHp,
            MaxEnemyHp: enemy.Status.MaxHp,
            Exp: exp,
            IsLevelUp: isLevelUp);
    }

    internal static TurnResult ExecuteTurn(Player player, TrainingEnemy enemy, TurnResult turnResult)
    {
        var currentPlayerHp = turnResult.CurrentPlayerHp;
        var currentEnemyHp = turnResult.CurrentEnemyHp;

        var playerFirst = player.Status.Speed >= enemy.Status.Speed;
        if (playerFirst)
        {
            var playerUsesIntelligence = player.Status.Intelligence > player.Status.Strength;
            var playerAttackPower = playerUsesIntelligence ? player.Status.Intelligence : player.Status.Strength;
            var playerDefensePower = playerUsesIntelligence ? enemy.Status.Intelligence : enemy.Status.Defense;
            var playerDealtDamage = Math.Max(1, playerAttackPower - playerDefensePower);
            currentEnemyHp = Math.Max(0, currentEnemyHp - playerDealtDamage);
            if (currentEnemyHp > 0)
            {
                var enemyUsesIntelligence = enemy.Status.Intelligence > enemy.Status.Strength;
                var enemyAttackPower = enemyUsesIntelligence ? enemy.Status.Intelligence : enemy.Status.Strength;
                var enemyDefensePower = enemyUsesIntelligence ? player.Status.Intelligence : player.Status.Defense;
                var enemyDealtDamage = Math.Max(1, enemyAttackPower - enemyDefensePower);
                currentPlayerHp = Math.Max(0, currentPlayerHp - enemyDealtDamage);
            }
        }
        else
        {
            var enemyUsesIntelligence = enemy.Status.Intelligence > enemy.Status.Strength;
            var enemyAttackPower = enemyUsesIntelligence ? enemy.Status.Intelligence : enemy.Status.Strength;
            var enemyDefensePower = enemyUsesIntelligence ? player.Status.Intelligence : player.Status.Defense;
            var enemyDealtDamage = Math.Max(1, enemyAttackPower - enemyDefensePower);
            currentPlayerHp = Math.Max(0, currentPlayerHp - enemyDealtDamage);
            if (currentPlayerHp > 0)
            {
                var playerUsesIntelligence = player.Status.Intelligence > player.Status.Strength;
                var playerAttackPower = playerUsesIntelligence ? player.Status.Intelligence : player.Status.Strength;
                var playerDefensePower = playerUsesIntelligence ? enemy.Status.Intelligence : enemy.Status.Defense;
                var playerDealtDamage = Math.Max(1, playerAttackPower - playerDefensePower);
                currentEnemyHp = Math.Max(0, currentEnemyHp - playerDealtDamage);
            }
        }

        return turnResult with
        {
            Turn = turnResult.Turn + 1,
            CurrentPlayerHp = currentPlayerHp,
            CurrentEnemyHp = currentEnemyHp
        };
    }

    internal static int CalcExp(Player player, TrainingEnemy enemy, TurnResult turnResult)
    {
        var playerDealtTotalDamage = enemy.Status.MaxHp - turnResult.CurrentEnemyHp;
        var levelDiff = Math.Abs(player.Level - enemy.Level);

        var resultBonus = TrainingConstants.Exp.DrawMultiplier;
        if (turnResult.CurrentPlayerHp > 0 && turnResult.CurrentEnemyHp <= 0)
        {
            resultBonus = TrainingConstants.Exp.WinMultiplier;
        }
        else if (turnResult.CurrentPlayerHp <= 0 && turnResult.CurrentEnemyHp > 0)
        {
            resultBonus = TrainingConstants.Exp.LoseMultiplier;
        }

        var baseExp = Math.Max(
            TrainingConstants.Exp.BaseExpMin,
            playerDealtTotalDamage / TrainingConstants.Exp.DamageBaseDivisor);

        var levelBonus = TrainingConstants.Exp.LevelBonusLow;
        if (levelDiff <= TrainingConstants.Exp.LevelBonusHighThreshold)
        {
            levelBonus = TrainingConstants.Exp.LevelBonusHigh;
        }
        else if (levelDiff <= TrainingConstants.Exp.LevelBonusMidThreshold)
        {
            levelBonus = TrainingConstants.Exp.LevelBonusMid;
        }

        var exp = (int)Math.Floor(baseExp * (levelBonus + resultBonus));
        return Math.Max(1, exp);
    }

    private static bool ApplyExp(Player player, int exp)
    {
        player.GainExp(exp);
        return player.LevelUp();
    }
}
