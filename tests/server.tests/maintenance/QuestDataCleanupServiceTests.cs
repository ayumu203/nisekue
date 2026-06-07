using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using server.application.maintenance;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using server.infrastructure.quest.room;
using server.infrastructure.quest.run;
using server.tests;
using Xunit;

namespace server.tests.maintenance;

public class QuestDataCleanupServiceTests
{
    [Fact]
    public async Task DeleteOldDataAsync_WhenOldQuestDataExists_DeletesAllRelatedData()
    {
        var now = DateTimeOffset.UtcNow;
        var cutoffDate = now.AddDays(-7);

        var roomId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
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
            seedContext.QuestRooms.Add(new QuestRoomEntity
            {
                Id = roomId,
                OwnerPlayerId = playerId,
                StageId = 1,
                Mode = 1,
                Status = 2,
                Version = 1,
                CreatedAt = now.AddDays(-14),
                ClosedAt = now.AddDays(-13)
            });
            seedContext.QuestRoomAllowedPlayers.Add(new QuestRoomAllowedPlayerEntity
            {
                RoomId = roomId,
                PlayerId = playerId,
                AddedAt = now.AddDays(-14)
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
                JoinedAt = now.AddDays(-14)
            });
            seedContext.QuestRuns.Add(new QuestRunEntity
            {
                Id = runId,
                RoomId = roomId,
                StageId = 1,
                Status = 2,
                CurrentFloorNo = 1,
                CurrentTurnNo = 1,
                ActionDeadlineAt = now.AddDays(-13),
                ChatMessagesJson = "[]",
                StartedAt = now.AddDays(-14),
                EndedAt = now.AddDays(-13)
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
                UpdatedAt = now.AddDays(-14)
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
                SubmittedAt = now.AddDays(-14),
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
                Gold = 100,
                SkippedRewardPlayerIdsJson = "[]"
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new QuestDataCleanupService(new TestDbContextFactory(connection));

        var result = await service.DeleteOldDataAsync(cutoffDate);

        result.DeletedQuestTurnCommands.Should().Be(1);
        result.DeletedQuestFloorTraps.Should().Be(1);
        result.DeletedQuestRunEnemies.Should().Be(1);
        result.DeletedQuestRunPartyMembers.Should().Be(1);
        result.DeletedQuestRunPartySnapshots.Should().Be(1);
        result.DeletedQuestRewardSummaries.Should().Be(1);
        result.DeletedQuestRuns.Should().Be(1);
        result.DeletedQuestRoomParticipants.Should().Be(1);
        result.DeletedQuestRoomAllowedPlayers.Should().Be(1);
        result.DeletedQuestRooms.Should().Be(1);

        await using var verifyContext = CreateDbContext(connection);
        (await verifyContext.QuestTurnCommands.CountAsync()).Should().Be(0);
        (await verifyContext.QuestFloorTraps.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRunEnemies.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRunPartyMembers.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRunPartySnapshots.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRewardSummaries.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRuns.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRoomParticipants.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRoomAllowedPlayers.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRooms.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteOldDataAsync_WhenNoOldDataExists_DeletesNothing()
    {
        var now = DateTimeOffset.UtcNow;
        var cutoffDate = now.AddDays(-7);

        var roomId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
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
            seedContext.QuestRooms.Add(new QuestRoomEntity
            {
                Id = roomId,
                OwnerPlayerId = playerId,
                StageId = 1,
                Mode = 1,
                Status = 1,
                Version = 1,
                CreatedAt = now
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new QuestDataCleanupService(new TestDbContextFactory(connection));

        var result = await service.DeleteOldDataAsync(cutoffDate);

        result.DeletedQuestTurnCommands.Should().Be(0);
        result.DeletedQuestFloorTraps.Should().Be(0);
        result.DeletedQuestRunEnemies.Should().Be(0);
        result.DeletedQuestRunPartyMembers.Should().Be(0);
        result.DeletedQuestRunPartySnapshots.Should().Be(0);
        result.DeletedQuestRewardSummaries.Should().Be(0);
        result.DeletedQuestRuns.Should().Be(0);
        result.DeletedQuestRoomParticipants.Should().Be(0);
        result.DeletedQuestRoomAllowedPlayers.Should().Be(0);
        result.DeletedQuestRooms.Should().Be(0);

        await using var verifyContext = CreateDbContext(connection);
        (await verifyContext.QuestRooms.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteOldDataAsync_WhenMixedOldAndNewData_OnlyDeletesOldData()
    {
        var now = DateTimeOffset.UtcNow;
        var cutoffDate = now.AddDays(-7);

        var oldRoomId = Guid.NewGuid();
        var newRoomId = Guid.NewGuid();
        var oldRunId = Guid.NewGuid();
        var oldParticipantId = Guid.NewGuid();
        var newParticipantId = Guid.NewGuid();
        var oldPlayerId = Guid.NewGuid();
        var newPlayerId = Guid.NewGuid();

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();

            seedContext.Players.Add(new PlayerEntity
            {
                Id = oldPlayerId,
                Name = "old_player",
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
            seedContext.Players.Add(new PlayerEntity
            {
                Id = newPlayerId,
                Name = "new_player",
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

            seedContext.QuestRooms.Add(new QuestRoomEntity
            {
                Id = oldRoomId,
                OwnerPlayerId = oldPlayerId,
                StageId = 1,
                Mode = 1,
                Status = 2,
                Version = 1,
                CreatedAt = now.AddDays(-14),
                ClosedAt = now.AddDays(-13)
            });
            seedContext.QuestRooms.Add(new QuestRoomEntity
            {
                Id = newRoomId,
                OwnerPlayerId = newPlayerId,
                StageId = 1,
                Mode = 1,
                Status = 1,
                Version = 1,
                CreatedAt = now
            });
            seedContext.QuestRoomAllowedPlayers.Add(new QuestRoomAllowedPlayerEntity
            {
                RoomId = oldRoomId,
                PlayerId = oldPlayerId,
                AddedAt = now.AddDays(-14)
            });
            seedContext.QuestRoomAllowedPlayers.Add(new QuestRoomAllowedPlayerEntity
            {
                RoomId = newRoomId,
                PlayerId = newPlayerId,
                AddedAt = now
            });
            seedContext.QuestRoomParticipants.Add(new QuestRoomParticipantEntity
            {
                Id = oldParticipantId,
                RoomId = oldRoomId,
                ParticipantType = 1,
                PlayerId = oldPlayerId,
                DisplayName = "old",
                BattleRow = 1,
                BattleColumn = 1,
                ParticipantStatus = 1,
                IsOwner = true,
                JoinedAt = now.AddDays(-14)
            });
            seedContext.QuestRoomParticipants.Add(new QuestRoomParticipantEntity
            {
                Id = newParticipantId,
                RoomId = newRoomId,
                ParticipantType = 1,
                PlayerId = newPlayerId,
                DisplayName = "new",
                BattleRow = 1,
                BattleColumn = 1,
                ParticipantStatus = 1,
                IsOwner = true,
                JoinedAt = now
            });
            seedContext.QuestRuns.Add(new QuestRunEntity
            {
                Id = oldRunId,
                RoomId = oldRoomId,
                StageId = 1,
                Status = 2,
                CurrentFloorNo = 1,
                CurrentTurnNo = 1,
                ActionDeadlineAt = now.AddDays(-13),
                ChatMessagesJson = "[]",
                StartedAt = now.AddDays(-14),
                EndedAt = now.AddDays(-13)
            });
            seedContext.QuestRunPartySnapshots.Add(new QuestRunPartySnapshotEntity
            {
                RunId = oldRunId,
                ParticipantId = oldParticipantId,
                ParticipantType = 1,
                DisplayName = "old",
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
                RunId = oldRunId,
                ParticipantId = oldParticipantId,
                CurrentHp = 10,
                CurrentMp = 5,
                IsDead = false,
                CanActFromTurn = 1,
                ActionMode = 1,
                HasLeftQuest = false,
                IsManualControlRequested = false,
                UpdatedAt = now.AddDays(-14)
            });
            seedContext.QuestRunEnemies.Add(new QuestRunEnemyEntity
            {
                RunId = oldRunId,
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
                RunId = oldRunId,
                TurnNo = 1,
                ParticipantId = oldParticipantId,
                ActionKind = 1,
                SubmittedAt = now.AddDays(-14),
                IsAutoSubmitted = false
            });
            seedContext.QuestFloorTraps.Add(new QuestFloorTrapEntity
            {
                RunId = oldRunId,
                TrapId = Guid.NewGuid(),
                SourceParticipantId = oldParticipantId,
                MoveId = 1,
                ExpiresAfterFloorNo = 2,
                IsTriggered = false
            });
            seedContext.QuestRewardSummaries.Add(new QuestRewardSummaryEntity
            {
                RunId = oldRunId,
                Exp = 10,
                Gold = 100,
                SkippedRewardPlayerIdsJson = "[]"
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new QuestDataCleanupService(new TestDbContextFactory(connection));

        var result = await service.DeleteOldDataAsync(cutoffDate);

        result.DeletedQuestRooms.Should().Be(1);
        result.DeletedQuestRoomParticipants.Should().Be(1);
        result.DeletedQuestRoomAllowedPlayers.Should().Be(1);
        result.DeletedQuestRuns.Should().Be(1);
        result.DeletedQuestTurnCommands.Should().Be(1);
        result.DeletedQuestFloorTraps.Should().Be(1);
        result.DeletedQuestRunEnemies.Should().Be(1);
        result.DeletedQuestRunPartyMembers.Should().Be(1);
        result.DeletedQuestRunPartySnapshots.Should().Be(1);
        result.DeletedQuestRewardSummaries.Should().Be(1);

        await using var verifyContext = CreateDbContext(connection);
        (await verifyContext.QuestRooms.CountAsync()).Should().Be(1);
        (await verifyContext.QuestRoomParticipants.CountAsync()).Should().Be(1);
        (await verifyContext.QuestRoomAllowedPlayers.CountAsync()).Should().Be(1);
        (await verifyContext.QuestRuns.CountAsync()).Should().Be(0);
        var remainingRoom = await verifyContext.QuestRooms.SingleAsync();
        remainingRoom.Id.Should().Be(newRoomId);
    }

    [Fact]
    public async Task DeleteOldDataAsync_WhenOldRoomHasNoRuns_DeletesRoomAndParticipantsOnly()
    {
        var now = DateTimeOffset.UtcNow;
        var cutoffDate = now.AddDays(-7);

        var roomId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
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
            seedContext.QuestRooms.Add(new QuestRoomEntity
            {
                Id = roomId,
                OwnerPlayerId = playerId,
                StageId = 1,
                Mode = 1,
                Status = 2,
                Version = 1,
                CreatedAt = now.AddDays(-14),
                ClosedAt = now.AddDays(-13)
            });
            seedContext.QuestRoomAllowedPlayers.Add(new QuestRoomAllowedPlayerEntity
            {
                RoomId = roomId,
                PlayerId = playerId,
                AddedAt = now.AddDays(-14)
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
                JoinedAt = now.AddDays(-14)
            });
            await seedContext.SaveChangesAsync();
        }

        var service = new QuestDataCleanupService(new TestDbContextFactory(connection));

        var result = await service.DeleteOldDataAsync(cutoffDate);

        result.DeletedQuestTurnCommands.Should().Be(0);
        result.DeletedQuestFloorTraps.Should().Be(0);
        result.DeletedQuestRunEnemies.Should().Be(0);
        result.DeletedQuestRunPartyMembers.Should().Be(0);
        result.DeletedQuestRunPartySnapshots.Should().Be(0);
        result.DeletedQuestRewardSummaries.Should().Be(0);
        result.DeletedQuestRuns.Should().Be(0);
        result.DeletedQuestRoomParticipants.Should().Be(1);
        result.DeletedQuestRoomAllowedPlayers.Should().Be(1);
        result.DeletedQuestRooms.Should().Be(1);

        await using var verifyContext = CreateDbContext(connection);
        (await verifyContext.QuestRoomParticipants.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRoomAllowedPlayers.CountAsync()).Should().Be(0);
        (await verifyContext.QuestRooms.CountAsync()).Should().Be(0);
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection) => TestHelpers.CreateSqliteDbContext(connection);

    private sealed class TestDbContextFactory(SqliteConnection connection) : TestHelpers.SqliteDbContextFactory(connection);
}
