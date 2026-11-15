using FluentAssertions;
using server.application.quest;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class MirrorEnemyActionPolicyTests
{
    private static readonly BattlePosition FrontLeft = new(BattleRow.Front, BattleColumn.Left);
    private static readonly BattlePosition FrontRight = new(BattleRow.Front, BattleColumn.Right);
    private static readonly BattlePosition BackLeft = new(BattleRow.Back, BattleColumn.Left);
    private static readonly BattlePosition BackRight = new(BattleRow.Back, BattleColumn.Right);

    [Fact]
    public void SelectAction_WarriorWithoutBuff_UsesSelfBuff()
    {
        var buff = CreateSelfBuffMove(201, BuffStat.Strength);
        var attack = CreateAttackMove(202, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Warrior, FrontLeft, [buff, attack]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(201);
    }

    [Fact]
    public void SelectAction_WarriorWithBuffAlreadyActive_Attacks()
    {
        var buff = CreateSelfBuffMove(201, BuffStat.Strength);
        var attack = CreateAttackMove(202, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Warrior, FrontLeft, [buff, attack], buffs: [new BattleBuffState(BuffStat.Strength, BuffCalculationType.Mul, 1.5m, 3)]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(202);
    }

    [Fact]
    public void SelectAction_WithRandomBelowRate_PicksSecondCandidate()
    {
        var buff = CreateSelfBuffMove(201, BuffStat.Strength);
        var attack = CreateAttackMove(202, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Warrior, FrontLeft, [buff, attack]);

        var decision = scenario.Select(() => 0.0);

        decision.Action.MoveId.Should().Be(202);
    }

    [Fact]
    public void SelectAction_SingleAttack_TargetsLowestHpOpponent()
    {
        var attack = CreateAttackMove(202, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddOpponent(FrontRight, hp: 10)
            .AddEnemy(Job.Warrior, FrontLeft, [attack]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(202);
        decision.Action.SelectedPosition.Should().Be(FrontRight);
    }

    [Fact]
    public void SelectAction_WithoutUsableMove_NormalAttacksLowestHpOpponent()
    {
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddOpponent(FrontRight, hp: 10)
            .AddEnemy(Job.Apprentice, FrontLeft, []);

        var decision = scenario.Select(NeverSecond);

        decision.Action.Kind.Should().Be(BattleActionKind.NormalAttack);
        decision.Action.SelectedPosition.Should().Be(FrontRight);
    }

    [Fact]
    public void SelectAction_MageWithTwoTargetsInRow_PrefersAreaAttack()
    {
        var single = CreateAttackMove(301, AttackRange.Single, power: 1.5m);
        var row = CreateAttackMove(302, AttackRange.Row, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddOpponent(FrontRight, hp: 100)
            .AddEnemy(Job.Mage, BackLeft, [single, row]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(302);
    }

    [Fact]
    public void SelectAction_GuardianWithCoverMove_UsesCoverEvenWhenAlliesAreHealthy()
    {
        var cover = CreateSelfAilmentMove(401, AilmentType.CoverAll);
        var attack = CreateAttackMove(402, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Guardian, FrontLeft, [cover, attack])
            .AddEnemy(Job.Mage, BackLeft, []);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(401);
    }

    [Fact]
    public void SelectAction_GuardianAlreadyCovering_DoesNotRepeatCover()
    {
        var cover = CreateSelfAilmentMove(401, AilmentType.CoverAll);
        var attack = CreateAttackMove(402, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Guardian, FrontLeft, [cover, attack], ailments: [new BattleAilmentState(AilmentType.CoverAll, 2)]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(402);
    }

    [Fact]
    public void SelectAction_GuardianWithoutCoverMove_UsesTaunt()
    {
        var taunt = CreateSelfAilmentMove(403, AilmentType.Taunt);
        var attack = CreateAttackMove(402, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Guardian, FrontLeft, [taunt, attack]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(403);
    }

    [Fact]
    public void SelectAction_GuardianWithCoverAndTaunt_DoesNotUseTauntWhileCovering()
    {
        var cover = CreateSelfAilmentMove(401, AilmentType.CoverAll);
        var taunt = CreateSelfAilmentMove(403, AilmentType.Taunt);
        var attack = CreateAttackMove(402, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Guardian, FrontLeft, [cover, taunt, attack], ailments: [new BattleAilmentState(AilmentType.CoverAll, 2)]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(402);
    }

    [Fact]
    public void SelectAction_PriestWithDeadAlly_Revives()
    {
        var revive = CreateReviveMove(501);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Priest, BackLeft, [revive])
            .AddEnemy(Job.Warrior, FrontLeft, [], hp: 0, isDead: true);

        var decision = scenario.Select(NeverSecond);

        decision.IsRevive.Should().BeTrue();
        decision.Action.MoveId.Should().Be(501);
        decision.Action.SelectedPosition.Should().Be(FrontLeft);
    }

    [Fact]
    public void SelectAction_PriestAfterThreeRevivesOnFloor_DoesNotRevive()
    {
        var revive = CreateReviveMove(501);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Priest, BackLeft, [revive], reviveUsedCount: MirrorEnemyActionPolicy.MaxRevivesPerFloor)
            .AddEnemy(Job.Warrior, FrontLeft, [], hp: 0, isDead: true);

        var decision = scenario.Select(NeverSecond);

        decision.IsRevive.Should().BeFalse();
        decision.Action.Kind.Should().Be(BattleActionKind.NormalAttack);
    }

    [Fact]
    public void SelectAction_PriestWithInjuredAlly_Heals()
    {
        var heal = CreateHealMove(502);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Priest, BackLeft, [heal])
            .AddEnemy(Job.Warrior, FrontLeft, [], hp: 20);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(502);
        decision.Action.SelectedPosition.Should().Be(FrontLeft);
    }

    [Fact]
    public void SelectAction_SelfHarmBuffBelowTwentyPercentHp_IsNotUsed()
    {
        var selfHarmBuff = CreateSelfHarmBuffMove(601);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Warrior, FrontLeft, [selfHarmBuff], hp: 19);

        var decision = scenario.Select(NeverSecond);

        decision.Action.Kind.Should().Be(BattleActionKind.NormalAttack);
    }

    [Fact]
    public void SelectAction_SelfHarmBuffAtTwentyPercentHp_IsUsed()
    {
        var selfHarmBuff = CreateSelfHarmBuffMove(601);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Warrior, FrontLeft, [selfHarmBuff], hp: 20);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(601);
    }

    [Fact]
    public void SelectAction_RangerWhenOpponentAlreadyHasTrap_SkipsTrap()
    {
        var trap = CreateEnemyAilmentMove(701, AilmentType.DamageTrap);
        var attack = CreateAttackMove(702, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100, ailments: [new BattleAilmentState(AilmentType.DamageTrap, 2, new DamageEffect(1, 0.1m, 0, 0m, ElementType.None))])
            .AddEnemy(Job.Ranger, BackLeft, [trap, attack]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(702);
    }

    [Fact]
    public void SelectAction_RangerWithoutTrapOnOpponents_SetsTrap()
    {
        var trap = CreateEnemyAilmentMove(701, AilmentType.DamageTrap);
        var attack = CreateAttackMove(702, AttackRange.Single, power: 1m);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Ranger, BackLeft, [trap, attack]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(701);
    }

    [Fact]
    public void SelectAction_RangerStatusAilment_SkipsOpponentsThatAlreadyHaveIt()
    {
        var poison = CreateEnemyAilmentMove(703, AilmentType.Poison);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 10, ailments: [new BattleAilmentState(AilmentType.Poison, 2)])
            .AddOpponent(FrontRight, hp: 100)
            .AddEnemy(Job.Ranger, BackLeft, [poison]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.MoveId.Should().Be(703);
        decision.Action.SelectedPosition.Should().Be(FrontRight);
    }

    [Fact]
    public void SelectAction_SelfKnockoutMove_IsNeverUsed()
    {
        var selfDestruct = CreateSelfDestructMove(801);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Warrior, FrontLeft, [selfDestruct]);

        var decision = scenario.Select(NeverSecond);

        decision.Action.Kind.Should().Be(BattleActionKind.NormalAttack);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenMirrorPriestRevives_RecordsReviveOnEnemyState()
    {
        var revive = CreateReviveMove(501);
        var scenario = new Scenario()
            .AddOpponent(FrontLeft, hp: 100)
            .AddEnemy(Job.Priest, BackLeft, [revive])
            .AddEnemy(Job.Warrior, FrontLeft, [], hp: 0, isDead: true);
        var run = scenario.BuildRun();
        var factory = new QuestBattleFactory(new MirrorEnemyActionPolicy(NeverSecond));

        var (actions, _) = await factory.CreateTurnInputsAsync(
            run,
            new FakeMoveRepository([revive]),
            run.MirrorEnemyPlan!.BuildEnemyDefinitions());

        var priest = run.BattleState.Enemies.First();
        actions.Should().Contain(action => action.ActorId == priest.Id.Value && action.MoveId == 501);
        priest.ReviveUsedCount.Should().Be(1);
    }

    private static double NeverSecond() => 1.0;

    private static Move CreateAttackMove(int id, AttackRange range, decimal power)
    {
        var moveId = new MoveId(id);
        return new Move(
            moveId,
            $"Attack{id}",
            "attack",
            TargetType.Enemy,
            range,
            1,
            0,
            MoveCategory.Attack,
            effects: [new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Damage, damage: new DamageEffect(1, power, 0, 0m, ElementType.None))]);
    }

    private static Move CreateSelfBuffMove(int id, BuffStat stat)
    {
        var moveId = new MoveId(id);
        return new Move(
            moveId,
            $"Buff{id}",
            "buff",
            TargetType.Self,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects: [new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Buff, buff: new BuffEffect(stat, BuffCalculationType.Mul, 1.5m, 3, 1m, false))]);
    }

    private static Move CreateSelfHarmBuffMove(int id)
    {
        var moveId = new MoveId(id);
        return new Move(
            moveId,
            $"SelfHarm{id}",
            "self harm buff",
            TargetType.Self,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.HalveSelfHp),
                new MoveEffect(new MoveEffectId(2), moveId, 2, MoveEffectType.Buff, buff: new BuffEffect(BuffStat.Strength, BuffCalculationType.Mul, 2m, 3, 1m, false))
            ]);
    }

    private static Move CreateSelfDestructMove(int id)
    {
        var moveId = new MoveId(id);
        return new Move(
            moveId,
            $"SelfDestruct{id}",
            "self destruct",
            TargetType.Enemy,
            AttackRange.All,
            1,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Damage, damage: new DamageEffect(1, 5m, 0, 0m, ElementType.None)),
                new MoveEffect(new MoveEffectId(2), moveId, 2, MoveEffectType.Knockout, overrideTargetType: TargetType.Self, overrideAttackRange: AttackRange.Single)
            ]);
    }

    private static Move CreateSelfAilmentMove(int id, AilmentType ailmentType)
    {
        var moveId = new MoveId(id);
        return new Move(
            moveId,
            $"SelfAilment{id}",
            "self ailment",
            TargetType.Self,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects: [new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Ailment, ailment: new AilmentEffect(ailmentType, 1m, 2))]);
    }

    private static Move CreateEnemyAilmentMove(int id, AilmentType ailmentType)
    {
        var moveId = new MoveId(id);
        var triggerDamage = ailmentType is AilmentType.DamageTrap or AilmentType.PoisonTrap
            ? new DamageEffect(1, 0.1m, 0, 0m, ElementType.None)
            : null;
        return new Move(
            moveId,
            $"EnemyAilment{id}",
            "enemy ailment",
            TargetType.Enemy,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects: [new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Ailment, ailment: new AilmentEffect(ailmentType, 1m, 2, triggerDamage))]);
    }

    private static Move CreateReviveMove(int id)
    {
        var moveId = new MoveId(id);
        return new Move(
            moveId,
            $"Revive{id}",
            "revive",
            TargetType.Ally,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            targetLifeState: TargetLifeState.Dead,
            effects: [new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Heal, damage: new DamageEffect(1, 0.5m, 0, 0m, ElementType.None))]);
    }

    private static Move CreateHealMove(int id)
    {
        var moveId = new MoveId(id);
        return new Move(
            moveId,
            $"Heal{id}",
            "heal",
            TargetType.Ally,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects: [new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Heal, damage: new DamageEffect(1, 1m, 0, 0m, ElementType.None))]);
    }

    /// <summary>味方（プレイヤー側）と名鑑の祠の敵を並べて、最初に追加した敵の行動を選ぶためのヘルパー。</summary>
    private sealed class Scenario
    {
        private readonly List<(QuestRunPartyMemberSnapshot Snapshot, QuestRunPartyMemberState State)> opponents = [];
        private readonly List<(MirrorEnemySnapshot Snapshot, QuestEnemyState State, IReadOnlyList<Move> Moves)> enemies = [];

        public Scenario AddOpponent(BattlePosition position, int hp, IEnumerable<BattleAilmentState>? ailments = null)
        {
            var participantId = QuestParticipantId.New();
            var snapshot = new QuestRunPartyMemberSnapshot(
                participantId,
                ParticipantType.Player,
                $"Player{opponents.Count + 1}",
                null,
                Job.Warrior,
                new Status(100, 10, 10, 10, 10, 10, 10),
                weaponEquipmentId: null,
                armorEquipmentId: null,
                new MoveSet(),
                position,
                ActionMode.Manual);
            var state = new QuestRunPartyMemberState(participantId, hp, 10, isDead: false, canActFromTurn: 1, ActionMode.Manual, ailments: ailments);
            opponents.Add((snapshot, state));
            return this;
        }

        public Scenario AddEnemy(
            Job job,
            BattlePosition position,
            IReadOnlyList<Move> moves,
            int hp = 100,
            bool isDead = false,
            int reviveUsedCount = 0,
            IEnumerable<BattleAilmentState>? ailments = null,
            IEnumerable<BattleBuffState>? buffs = null)
        {
            var definitionId = new QuestEnemyDefinitionId(MirrorEnemyPlanBuilder.FirstDefinitionId + enemies.Count);
            var snapshot = new MirrorEnemySnapshot(
                definitionId,
                new PlayerId(Guid.NewGuid()),
                $"Enemy{enemies.Count + 1}",
                "image/character/ch001_bmnpc.png",
                job,
                100,
                0,
                new Status(100, 100, 50, 10, 50, 10, 10),
                moves.Select(x => x.Id));
            var state = new QuestEnemyState(
                QuestEnemyInstanceId.New(),
                definitionId,
                position,
                hp,
                currentMp: 100,
                isDead,
                ailments,
                buffs,
                reviveUsedCount: reviveUsedCount);
            enemies.Add((snapshot, state, moves));
            return this;
        }

        public MirrorEnemyActionDecision Select(Func<double> nextDouble)
        {
            var run = BuildRun();
            var plan = run.MirrorEnemyPlan!;
            var (snapshot, _, moves) = enemies[0];
            var actor = run.BattleState.Enemies.First();
            return new MirrorEnemyActionPolicy(nextDouble).SelectAction(run, actor, snapshot, moves, plan.BuildEnemyDefinitions());
        }

        public QuestRun BuildRun()
        {
            var plan = new MirrorEnemyPlan(
                enemies.Select(x => x.Snapshot),
                [new MirrorFloorPlan(1, isBoss: false, enemies.Select((x, index) => new QuestEnemyPlacement(index + 1, x.Snapshot.DefinitionId, x.State.Position)))]);

            return new QuestRun(
                QuestRunId.New(),
                QuestRoomId.New(),
                new QuestStageId(14),
                opponents.Select(x => x.Snapshot).ToArray(),
                new QuestFloorState(1, false, []),
                new QuestBattleState(opponents.Select(x => x.State).ToArray(), enemies.Select(x => x.State).ToArray()),
                new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30)),
                new QuestTrapCollection(),
                new QuestRewardAccumulator(),
                lastTurnResults: null,
                chatMessages: [],
                startedAt: DateTimeOffset.UtcNow,
                mirrorEnemyPlan: plan);
        }
    }

    private sealed class FakeMoveRepository(IReadOnlyList<Move> moves) : IMoveRepository
    {
        public Task<Move?> GetMoveAsync(MoveId moveId)
            => Task.FromResult(moves.FirstOrDefault(x => x.Id.Id == moveId.Id));

        public Task<IReadOnlyList<Move>> GetAllMovesAsync()
            => Task.FromResult(moves);
    }
}
