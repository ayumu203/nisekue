using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using server.domain.battle;
using server.domain.battle.enums;
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

    private static QuestRun CreateRun()
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
            pet: null);

        var enemyId = QuestEnemyInstanceId.New();
        return new QuestRun(
            QuestRunId.New(),
            QuestRoomId.New(),
            new QuestStageId(1),
            [snapshot],
            new QuestFloorState(1, false, []),
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
            new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30)),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            lastTurnResults: null,
            chatMessages: [],
            startedAt: DateTimeOffset.UtcNow);
    }
}
