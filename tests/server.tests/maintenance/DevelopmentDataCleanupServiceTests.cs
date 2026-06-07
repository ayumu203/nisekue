using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using server.application.maintenance;
using server.infrastructure;
using server.infrastructure.quest.room;
using server.infrastructure.quest.run;
using Xunit;

namespace server.tests.maintenance;

public class DevelopmentDataCleanupServiceTests
{
    [Fact]
    public async Task CleanupAsync_WhenRuntimeDataExists_DeletesAllTrackedGameData()
    {
        var databaseName = $"cleanup-{Guid.NewGuid()}";
        await using var seedContext = CreateDbContext(databaseName);
        await seedContext.Database.EnsureCreatedAsync();

        var roomId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        seedContext.QuestRooms.Add(new QuestRoomEntity
        {
            Id = roomId,
            OwnerPlayerId = Guid.NewGuid(),
            StageId = 1,
            Mode = 1,
            Status = 1,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow
        });
        seedContext.QuestRoomParticipants.Add(new QuestRoomParticipantEntity
        {
            Id = participantId,
            RoomId = roomId,
            ParticipantType = 1,
            PlayerId = Guid.NewGuid(),
            DisplayName = "tester",
            BattleRow = 1,
            BattleColumn = 1,
            ParticipantStatus = 1,
            IsOwner = true,
            JoinedAt = DateTimeOffset.UtcNow
        });
        seedContext.QuestRuns.Add(new QuestRunEntity
        {
            Id = runId,
            RoomId = roomId,
            StageId = 1,
            Status = 1,
            CurrentFloorNo = 1,
            CurrentTurnNo = 1,
            ActionDeadlineAt = DateTimeOffset.UtcNow.AddMinutes(1),
            ChatMessagesJson = "[]",
            StartedAt = DateTimeOffset.UtcNow
        });
        seedContext.QuestRunPartySnapshots.Add(new QuestRunPartySnapshotEntity
        {
            RunId = runId,
            ParticipantId = participantId,
            ParticipantType = 1,
            DisplayName = "tester",
            Job = 1,
            StartRow = 1,
            StartColumn = 1,
            MaxHp = 10,
            MaxMp = 5,
            Strength = 3,
            Defense = 2,
            Intelligence = 1,
            Luck = 1,
            Speed = 1,
            InitialActionMode = 1
        });
        seedContext.QuestRunPartyMembers.Add(new QuestRunPartyMemberEntity
        {
            RunId = runId,
            ParticipantId = participantId,
            CurrentHp = 10,
            CurrentMp = 5,
            IsDead = false,
            CanActFromTurn = 1,
            ActionMode = 1,
            HasLeftQuest = false,
            IsManualControlRequested = false,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        seedContext.QuestRunEnemies.Add(new QuestRunEnemyEntity
        {
            RunId = runId,
            EnemyInstanceId = Guid.NewGuid(),
            FloorNo = 1,
            EnemyDefinitionId = 1,
            BattleRow = 1,
            BattleColumn = 1,
            CurrentHp = 10,
            CurrentMp = 0,
            IsDead = false
        });
        seedContext.QuestTurnCommands.Add(new QuestTurnCommandEntity
        {
            RunId = runId,
            TurnNo = 1,
            ParticipantId = participantId,
            ActionKind = 1,
            SubmittedAt = DateTimeOffset.UtcNow,
            IsAutoSubmitted = false
        });
        seedContext.QuestFloorTraps.Add(new QuestFloorTrapEntity
        {
            RunId = runId,
            TrapId = Guid.NewGuid(),
            SourceParticipantId = participantId,
            MoveId = 1,
            ExpiresAfterFloorNo = 2,
            IsTriggered = false
        });
        seedContext.QuestRewardSummaries.Add(new QuestRewardSummaryEntity
        {
            RunId = runId,
            Exp = 10,
            EquipmentRewardId = 1001,
            SkippedRewardPlayerIdsJson = "[]"
        });
        await seedContext.SaveChangesAsync();

        var service = new DevelopmentDataCleanupService(new TestDbContextFactory(databaseName));

        var result = await service.CleanupAsync();

        result.DeletedQuestRooms.Should().Be(1);
        result.DeletedQuestRoomParticipants.Should().Be(1);
        result.DeletedQuestRuns.Should().Be(1);
        result.DeletedQuestRunPartySnapshots.Should().Be(1);
        result.DeletedQuestRunPartyMembers.Should().Be(1);
        result.DeletedQuestRunEnemies.Should().Be(1);
        result.DeletedQuestTurnCommands.Should().Be(1);
        result.DeletedQuestFloorTraps.Should().Be(1);
        result.DeletedQuestRewardSummaries.Should().Be(1);

        await using var verifyContext = CreateDbContext(databaseName);
        (await verifyContext.QuestRooms.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRoomParticipants.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRuns.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRunPartySnapshots.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRunPartyMembers.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRunEnemies.CountAsync()).Should().Be(0);
        (await verifyContext.QuestTurnCommands.CountAsync()).Should().Be(0);
        (await verifyContext.QuestFloorTraps.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRewardSummaries.CountAsync()).Should().Be(0);
    }

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => DevelopmentDataCleanupServiceTests.CreateDbContext(databaseName);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(DevelopmentDataCleanupServiceTests.CreateDbContext(databaseName));
    }
}
