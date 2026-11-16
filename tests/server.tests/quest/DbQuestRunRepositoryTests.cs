using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using server.infrastructure.quest.run;
using server.tests;
using Xunit;

namespace server.tests.quest;

public class DbQuestRunRepositoryTests
{
    [Fact]
    public async Task GetForResolutionAsync_DoesNotLoadLastTurnResults_ButKeepsChat()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var run = CreateRun();
        run.SetLastTurnResults(new QuestLastTurnResults(7, DateTimeOffset.UtcNow));
        run.AddChatMessage(new QuestChatMessage(
            1, run.PartySnapshots[0].ParticipantId, "Owner", "owner.png", "hello", DateTimeOffset.UtcNow));
        await repository.SaveAsync(run);

        var loaded = await repository.GetForResolutionAsync(run.Id);

        loaded.Should().NotBeNull();
        loaded!.LastTurnResults.Should().BeNull();
        loaded.LastTurnResultsDirty.Should().BeFalse();
        loaded.ChatMessages.Should().ContainSingle().Which.Message.Should().Be("hello");
    }

    [Fact]
    public async Task SaveAsync_WhenRunNotDirty_PreservesStoredLastTurnResults()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var run = CreateRun();
        run.SetLastTurnResults(new QuestLastTurnResults(7, DateTimeOffset.UtcNow));
        await repository.SaveAsync(run);

        // 軽量ロード（last_turn_results_json 未取得 = ダーティでない）したランを保存しても既存値を温存する。
        var lightweight = await repository.GetForResolutionAsync(run.Id);
        lightweight!.LastTurnResults.Should().BeNull();
        await repository.SaveAsync(lightweight);

        var full = await repository.GetAsync(run.Id);
        full!.LastTurnResults.Should().NotBeNull();
        full.LastTurnResults!.TurnNo.Should().Be(7);
    }

    [Fact]
    public async Task ListExpiredAsync_DoesNotLoadLastTurnResults_AndPreservesOnSave()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var run = CreateRun();
        run.SetLastTurnResults(new QuestLastTurnResults(7, DateTimeOffset.UtcNow));
        await repository.SaveAsync(run);

        // 期限切れ取得は表示専用の last_turn_results_json を読まない（ダーティでない）。
        var expired = await repository.ListExpiredAsync(DateTimeOffset.UtcNow.AddMinutes(1));
        var loaded = expired.Should().ContainSingle().Subject;
        loaded.LastTurnResults.Should().BeNull();
        loaded.LastTurnResultsDirty.Should().BeFalse();

        // 軽量ロード由来でも保存時に既存の last_turn_results を上書きしない。
        await repository.SaveAsync(loaded);
        var full = await repository.GetAsync(run.Id);
        full!.LastTurnResults!.TurnNo.Should().Be(7);
    }

    [Fact]
    public async Task SaveAsync_WithPetDeployment_RoundTripsPetsAndDeployment()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var run = CreateRun(
        [
            new QuestPetSnapshot(new QuestEnemyDefinitionId(3), new Status(40, 5, 7, 4, 1, 1, 6)),
            new QuestPetSnapshot(new QuestEnemyDefinitionId(5), new Status(60, 8, 9, 6, 2, 2, 3))
        ]);
        await repository.SaveAsync(run);

        var participantId = run.PartySnapshots[0].ParticipantId;
        run.StartPetDeployment(QuestPetDeployment.Create(participantId, run.PartySnapshots[0].Pets, Guid.NewGuid));
        await repository.SaveAsync(run);

        var loaded = await repository.GetForResolutionAsync(run.Id);

        loaded.Should().NotBeNull();
        loaded!.PartySnapshots[0].Pets.Select(x => x.EnemyDefinitionId.Value).Should().Equal(3, 5);
        loaded.PartySnapshots[0].Pets[1].Status.MaxHp.Should().Be(60);
        loaded.PetSummonUsed.Should().BeTrue();
        loaded.IsPetDeploymentActive.Should().BeTrue();
        loaded.PetDeployment!.RemainingTurns.Should().Be(3);
        loaded.PetDeployment.Pets.Select(x => x.ActorId).Should().Equal(run.PetDeployment!.Pets.Select(x => x.ActorId));
        loaded.PetDeployment.Pets[1].Position.Should().Be(new BattlePosition(BattleRow.Front, BattleColumn.Right));
        loaded.PetDeployment.Pets[1].CurrentHp.Should().Be(60);
    }

    [Fact]
    public async Task GetRoomIdAsync_ReturnsRoomId_WhenRunExists_AndNull_WhenMissing()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var run = CreateRun();
        await repository.SaveAsync(run);

        (await repository.GetRoomIdAsync(run.Id)).Should().Be(run.RoomId);
        (await repository.GetRoomIdAsync(QuestRunId.New())).Should().BeNull();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task GetForResolutionAsync_WithStaticStage_RestoresBossFloorFromStageDefinition(int floorNo, bool expected)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var stage = CreateStaticStage();
        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection), new FakeQuestStageRepository(stage));
        var run = CreateRun(stageId: stage.Id, floorNo: floorNo);
        await repository.SaveAsync(run);

        var resolutionRun = await repository.GetForResolutionAsync(run.Id);
        var fullRun = await repository.GetAsync(run.Id);

        resolutionRun!.FloorState.IsBossFloor.Should().Be(expected);
        fullRun!.FloorState.IsBossFloor.Should().Be(expected);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public async Task GetForResolutionAsync_WithEndlessStage_RestoresBossFloorFromBossInterval(int floorNo, bool expected)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var stage = CreateEndlessStage();
        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection), new FakeQuestStageRepository(stage));
        var run = CreateRun(stageId: stage.Id, floorNo: floorNo);
        await repository.SaveAsync(run);

        var loaded = await repository.GetForResolutionAsync(run.Id);

        loaded!.FloorState.IsBossFloor.Should().Be(expected);
    }

    [Fact]
    public async Task ListExpiredAsync_WhenCurrentFloorIsBoss_RestoresBossFloor()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var stage = CreateStaticStage();
        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection), new FakeQuestStageRepository(stage));
        var run = CreateRun(stageId: stage.Id, floorNo: 2, deadlineAt: DateTimeOffset.UtcNow.AddSeconds(-1));
        await repository.SaveAsync(run);

        var expired = await repository.ListExpiredAsync(DateTimeOffset.UtcNow);

        expired.Should().ContainSingle().Which.FloorState.IsBossFloor.Should().BeTrue();
    }

    [Fact]
    public async Task GetForResolutionAsync_WhenStageIsUnknown_TreatsFloorAsNormal()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection), new FakeQuestStageRepository(CreateStaticStage()));
        var run = CreateRun(stageId: new QuestStageId(999), floorNo: 2);
        await repository.SaveAsync(run);

        var loaded = await repository.GetForResolutionAsync(run.Id);

        loaded!.FloorState.IsBossFloor.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WithMirrorEnemyPlan_RestoresPlanOnLoad()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection));
        var sourcePlayerId = new PlayerId(Guid.NewGuid());
        var definitionId = new QuestEnemyDefinitionId(1_000_001);
        var plan = new MirrorEnemyPlan(
            [
                new MirrorEnemySnapshot(
                    definitionId,
                    sourcePlayerId,
                    "Top",
                    "image/character/ch001_bmnpc.png",
                    Job.Mage,
                    100,
                    3,
                    new Status(500, 60, 70, 80, 90, 10, 20),
                    [new MoveId(101), new MoveId(102)])
            ],
            [
                new MirrorFloorPlan(
                    1,
                    isBoss: false,
                    [new QuestEnemyPlacement(1, definitionId, new BattlePosition(BattleRow.Back, BattleColumn.Right))]),
                new MirrorFloorPlan(
                    2,
                    isBoss: true,
                    [new QuestEnemyPlacement(1, definitionId, new BattlePosition(BattleRow.Back, BattleColumn.Left))])
            ]);
        var run = CreateRun(mirrorEnemyPlan: plan);
        await repository.SaveAsync(run);

        var loaded = await repository.GetForResolutionAsync(run.Id);

        loaded.Should().NotBeNull();
        var loadedPlan = loaded!.MirrorEnemyPlan;
        loadedPlan.Should().NotBeNull();
        var enemy = loadedPlan!.Enemies.Should().ContainSingle().Subject;
        enemy.DefinitionId.Should().Be(definitionId);
        enemy.SourcePlayerId.Should().Be(sourcePlayerId);
        enemy.Name.Should().Be("Top");
        enemy.Job.Should().Be(Job.Mage);
        enemy.Level.Should().Be(100);
        enemy.RebirthCount.Should().Be(3);
        enemy.Status.MaxHp.Should().Be(500);
        enemy.Status.Speed.Should().Be(20);
        enemy.MoveIds.Select(x => x.Id).Should().Equal(101, 102);
        loadedPlan.Floors.Should().HaveCount(2);
        loadedPlan.GetFloor(2).IsBoss.Should().BeTrue();
        loadedPlan.GetFloor(1).Placements.Single().Position.Should().Be(new BattlePosition(BattleRow.Back, BattleColumn.Right));
    }

    [Fact]
    public async Task SaveAsync_WithoutMirrorEnemyPlan_LoadsNullPlan()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection));
        var run = CreateRun();
        await repository.SaveAsync(run);

        var loaded = await repository.GetAsync(run.Id);

        loaded!.MirrorEnemyPlan.Should().BeNull();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task GetForResolutionAsync_WithMirrorEnemyPlan_RestoresBossFloorFromPlan(int floorNo, bool expected)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        // ステージ定義はフロア2をボスにしていない。名鑑の祠はラン開始時の計画を正とする。
        var stage = CreateStaticStage(bossFloorNo: 1);
        var repository = new DbQuestRunRepository(new TestHelpers.SqliteDbContextFactory(connection), new FakeQuestStageRepository(stage));
        var definitionId = new QuestEnemyDefinitionId(1_000_001);
        var plan = new MirrorEnemyPlan(
            [
                new MirrorEnemySnapshot(
                    definitionId,
                    new PlayerId(Guid.NewGuid()),
                    "Top",
                    "image/character/ch001_bmnpc.png",
                    Job.Mage,
                    100,
                    0,
                    new Status(500, 60, 70, 80, 90, 10, 20),
                    [new MoveId(101)])
            ],
            [
                new MirrorFloorPlan(
                    1,
                    isBoss: false,
                    [new QuestEnemyPlacement(1, definitionId, new BattlePosition(BattleRow.Back, BattleColumn.Right))]),
                new MirrorFloorPlan(
                    2,
                    isBoss: true,
                    [new QuestEnemyPlacement(1, definitionId, new BattlePosition(BattleRow.Back, BattleColumn.Left))])
            ]);
        var run = CreateRun(stageId: stage.Id, floorNo: floorNo, mirrorEnemyPlan: plan);
        await repository.SaveAsync(run);

        var loaded = await repository.GetForResolutionAsync(run.Id);

        loaded!.FloorState.IsBossFloor.Should().Be(expected);
    }

    private static QuestStageDefinition CreateStaticStage(int bossFloorNo = 2)
    {
        return new QuestStageDefinition(
            new QuestStageId(5),
            "boss-test",
            "Boss Test",
            "image/quest/test-battlefield.svg",
            1,
            minimumEntryLevel: null,
            1,
            6,
            [
                new QuestFloorDefinition(1, bossFloorNo == 1 ? FloorType.Boss : FloorType.Normal, [], new QuestFloorRewardRule(0, 0)),
                new QuestFloorDefinition(2, bossFloorNo == 2 ? FloorType.Boss : FloorType.Normal, [], new QuestFloorRewardRule(0, 0))
            ],
            equipmentRewards: [],
            itemRewards: [],
            isActive: true);
    }

    private static QuestStageDefinition CreateEndlessStage()
    {
        return new QuestStageDefinition(
            new QuestStageId(13),
            "endless-test",
            "Endless Test",
            "image/quest/test-battlefield.svg",
            1,
            minimumEntryLevel: null,
            1,
            1,
            [],
            equipmentRewards: [],
            itemRewards: [],
            isActive: true,
            progressionType: ProgressionType.Endless,
            endlessConfig: new QuestEndlessConfig(0.08, 1.6, 100, 10, 1.5m, 3.0m, 5, 10, 1, 3, 100m, 750m));
    }

    private sealed class FakeQuestStageRepository(QuestStageDefinition stage) : IQuestStageRepository
    {
        public Task<QuestStageDefinition?> GetAsync(QuestStageId id)
            => Task.FromResult(stage.Id == id ? stage : null);

        public Task<QuestStageDefinition?> GetByStageCodeAsync(string stageCode)
            => Task.FromResult(stage.StageCode == stageCode ? stage : null);

        public Task<IReadOnlyList<QuestStageDefinition>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<QuestStageDefinition>>([stage]);
    }

    private static QuestRun CreateRun(
        IReadOnlyList<QuestPetSnapshot>? pets = null,
        QuestStageId? stageId = null,
        int floorNo = 1,
        DateTimeOffset? deadlineAt = null,
        MirrorEnemyPlan? mirrorEnemyPlan = null)
    {
        var participantId = QuestParticipantId.New();
        var snapshot = new QuestRunPartyMemberSnapshot(
            participantId,
            ParticipantType.Player,
            "Owner",
            "owner.png",
            Job.Warrior,
            new Status(20, 5, 5, 3, 1, 1, 2),
            weaponEquipmentId: null,
            armorEquipmentId: null,
            new MoveSet(),
            new BattlePosition(BattleRow.Front, BattleColumn.Left),
            ActionMode.Manual,
            pets);

        var enemyId = QuestEnemyInstanceId.New();
        return new QuestRun(
            QuestRunId.New(),
            QuestRoomId.New(),
            stageId ?? new QuestStageId(1),
            [snapshot],
            new QuestFloorState(floorNo, false, []),
            new QuestBattleState(
                [
                    new QuestRunPartyMemberState(participantId, 20, 5, false, 1, ActionMode.Manual)
                ],
                [
                    new QuestEnemyState(
                        enemyId,
                        new QuestEnemyDefinitionId(1),
                        new BattlePosition(BattleRow.Front, BattleColumn.Right),
                        10,
                        0,
                        false)
                ]),
            new QuestTurnState(1, deadlineAt ?? DateTimeOffset.UtcNow.AddSeconds(30)),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            lastTurnResults: null,
            chatMessages: [],
            startedAt: DateTimeOffset.UtcNow,
            mirrorEnemyPlan: mirrorEnemyPlan);
    }
}
