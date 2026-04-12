using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using server.application.maintenance;
using server.domain.chat;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.chat;
using server.infrastructure.player;
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

        var playerId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        seedContext.Players.Add(new PlayerEntity
        {
            Id = playerId,
            Name = "tester",
            Job = Job.Apprentice,
            Level = 1,
            Exp = 0,
            JobLevel = 1,
            JobExp = 0,
            Gold = 100,
            MaxHp = 10,
            MaxMp = 5,
            Strength = 3,
            Defense = 2,
            Intelligence = 1,
            Luck = 1,
            Speed = 1
        });
        seedContext.PlayerMoves.Add(new PlayerMoveEntity { PlayerId = playerId, MoveId1 = 1 });
        seedContext.PlayerMasterJobs.Add(new PlayerMasterJobEntity { PlayerId = playerId, Job = Job.Apprentice, MasteredAt = DateTimeOffset.UtcNow });
        seedContext.PlayerEquipments.Add(new PlayerEquipmentEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            EquipmentId = 1001,
            EquipmentType = 1,
            EquipmentStatus = 1,
            Durability = 10,
            Mastery = 0,
            AcquiredAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        seedContext.PlayerItemStacks.Add(new PlayerItemStackEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            ItemId = 3001,
            Quantity = 5,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        seedContext.MarketListings.Add(new MarketListingEntity
        {
            Id = Guid.NewGuid(),
            SellerId = playerId,
            ItemId = 3001,
            ItemName = "体力の実",
            FlavorText = "test",
            Quantity = 5,
            RemainingQuantity = 5,
            UnitPrice = 10,
            ListedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14)
        });
        seedContext.MarketTradeHistories.Add(new MarketTradeHistoryEntity
        {
            Id = Guid.NewGuid(),
            SellerId = playerId,
            BuyerId = playerId,
            ItemIdentifier = "item:3001",
            Quantity = 1,
            UnitPrice = 10,
            PurchasedAt = DateTimeOffset.UtcNow
        });
        seedContext.ItemDeletionLogs.Add(new ItemDeletionLogEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            ItemIdentifier = "item:3001",
            Quantity = 1,
            Reason = "manual_delete",
            DeletedAt = DateTimeOffset.UtcNow
        });
        seedContext.ChatRooms.Add(new ChatRoomEntity(new PlayerId(playerId), 1));
        seedContext.ChatMessages.Add(new ChatMessageEntity(
            new PlayerId(playerId),
            1,
            ChatMessageSenderType.System,
            senderId: null,
            "hello",
            DateTimeOffset.UtcNow,
            isAlerted: true));
        seedContext.QuestRooms.Add(new QuestRoomEntity
        {
            Id = roomId,
            OwnerPlayerId = playerId,
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
            PlayerId = playerId,
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

        result.DeletedPlayers.Should().Be(1);
        result.DeletedChatRooms.Should().Be(1);
        result.DeletedChatMessages.Should().Be(1);
        result.DeletedPlayerMoves.Should().Be(1);
        result.DeletedPlayerMasterJobs.Should().Be(1);
        result.DeletedPlayerEquipments.Should().Be(1);
        result.DeletedPlayerItemStacks.Should().Be(1);
        result.DeletedMarketListings.Should().Be(1);
        result.DeletedMarketTradeHistories.Should().Be(1);
        result.DeletedItemDeletionLogs.Should().Be(1);
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
        (await verifyContext.Players.CountAsync()).Should().Be(0);
        (await verifyContext.ChatRooms.CountAsync()).Should().Be(0);
        (await verifyContext.ChatMessages.CountAsync()).Should().Be(0);
        (await verifyContext.PlayerMoves.CountAsync()).Should().Be(0);
        (await verifyContext.PlayerMasterJobs.CountAsync()).Should().Be(0);
        (await verifyContext.PlayerEquipments.CountAsync()).Should().Be(0);
        (await verifyContext.PlayerItemStacks.CountAsync()).Should().Be(0);
        (await verifyContext.MarketListings.CountAsync()).Should().Be(0);
        (await verifyContext.MarketTradeHistories.CountAsync()).Should().Be(0);
        (await verifyContext.ItemDeletionLogs.CountAsync()).Should().Be(0);
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
