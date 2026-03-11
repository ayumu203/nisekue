using server.application.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.training;

namespace server.application.training;

public class TrainingBattleFactory
{
    private static readonly Guid PlayerBattleActorId = Guid.Empty;
    private static readonly Guid EnemyBattleActorId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private const int PlayerTrainingMoveId = 900001;
    private const int EnemyTrainingMoveId = 900002;

    public Guid PlayerActorId => PlayerBattleActorId;
    public Guid EnemyActorId => EnemyBattleActorId;

    public BattleActorInput CreatePlayerActor(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return new BattleActorInput(
            PlayerBattleActorId,
            player.Name,
            BattleSide.Ally,
            player.Status,
            player.MoveSet,
            CurrentHp: player.Status.MaxHp,
            CurrentMp: player.Status.MaxMp);
    }

    public BattleActorInput CreateEnemyActor(TrainingEnemy enemy)
    {
        ArgumentNullException.ThrowIfNull(enemy);

        return new BattleActorInput(
            EnemyBattleActorId,
            enemy.Name,
            BattleSide.Enemy,
            enemy.Status,
            new MoveSet(CreateTrainingMoveSlots(EnemyTrainingMoveId)),
            CurrentHp: enemy.Status.MaxHp,
            CurrentMp: enemy.Status.MaxMp);
    }

    public BattleActionInput[] CreateTurnActions(
        BattleActorInput playerActor,
        Guid enemyActorId,
        Move playerMove)
    {
        ArgumentNullException.ThrowIfNull(playerMove);

        return
        [
            CreatePlayerTurnAction(playerActor, enemyActorId, playerMove),
            new BattleActionInput(EnemyBattleActorId, BattleActionKind.UseMove, EnemyTrainingMoveId, TargetType.Enemy, AttackRange.Single, [playerActor.ActorId])
        ];
    }

    public Move[] CreateTrainingMoves(TrainingEnemy enemy, IEnumerable<Move> playerMoves)
    {
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(playerMoves);

        var moveMap = new Dictionary<int, Move>();
        foreach (var move in playerMoves)
        {
            moveMap[move.Id.Id] = move;
        }

        moveMap[EnemyTrainingMoveId] = CreateTrainingNormalAttack(EnemyTrainingMoveId, enemy.Status);

        return moveMap.Values.ToArray();
    }

    private static BattleActionInput CreatePlayerTurnAction(
        BattleActorInput playerActor,
        Guid enemyActorId,
        Move playerMove)
    {
        var currentMp = playerActor.CurrentMp ?? playerActor.BaseStatus.MaxMp;
        if (currentMp < playerMove.MpCost)
        {
            return new BattleActionInput(
                playerActor.ActorId,
                BattleActionKind.Wait,
                null,
                TargetType.Self,
                AttackRange.Single);
        }

        return new BattleActionInput(
            playerActor.ActorId,
            BattleActionKind.UseMove,
            playerMove.Id.Id,
            playerMove.TargetType,
            playerMove.AttackRange,
            ResolveTargetActorIds(playerActor.ActorId, enemyActorId, playerMove.TargetType));
    }

    private static MoveId?[] CreateTrainingMoveSlots(int moveId)
    {
        var slots = new MoveId?[MoveSet.MaxSlots];
        slots[0] = new MoveId(moveId);
        return slots;
    }

    private static IReadOnlyList<Guid>? ResolveTargetActorIds(Guid playerActorId, Guid enemyActorId, TargetType targetType)
    {
        return targetType switch
        {
            TargetType.Enemy => [enemyActorId],
            TargetType.Self => [playerActorId],
            _ => null
        };
    }

    private static Move CreateTrainingNormalAttack(int moveId, Status status)
    {
        var moveIdentity = new MoveId(moveId);
        return new Move(
            moveIdentity,
            "特訓通常攻撃",
            "特訓用の通常攻撃",
            TargetType.Enemy,
            AttackRange.Single,
            mpCost: 0,
            executionPriority: 0,
            MoveCategory.Attack,
            [
                new MoveEffect(
                    new MoveEffectId(moveId),
                    moveIdentity,
                    sequence: 1,
                    MoveEffectType.Damage,
                    new DamageEffect(
                        hitCount: 1,
                        powerRate: 1m,
                        fixedValue: 0,
                        criticalRate: 0m,
                        elementType: ElementType.None,
                        attackStat: ResolveAttackStat(status)))
            ]);
    }

    private static BuffStat ResolveAttackStat(Status status)
    {
        return status.Intelligence > status.Strength
            ? BuffStat.Intelligence
            : BuffStat.Strength;
    }
}
