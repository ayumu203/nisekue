using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class QuestRoomTests
{
    [Fact]
    public void Constructor_WhenVersionIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());

        var act = () => new QuestRoom(
            QuestRoomId.New(),
            ownerId,
            new QuestStageId(1),
            QuestRoomMode.Solo,
            new FormationLayout(),
            version: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddPlayer_WhenFirstPlayerAdded_SetsOwnerFlagAndAssignsPosition()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);

        room.AddPlayer(ownerId, "Owner");

        room.Participants.Should().ContainSingle();
        room.Participants[0].IsOwner.Should().BeTrue();
        room.Participants[0].Position.Should().Be(new BattlePosition(BattleRow.Front, BattleColumn.Left));
    }

    [Fact]
    public void AddPlayer_WhenSamePlayerJoinsTwice_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");

        var act = () => room.AddPlayer(ownerId, "Owner");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddPlayer_WhenCapacityExceeded_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        for (var i = 0; i < 5; i++)
        {
            room.AddPlayer(new PlayerId(Guid.NewGuid()), $"Member{i}");
        }

        var act = () => room.AddPlayer(new PlayerId(Guid.NewGuid()), "Overflow");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddPlayer_WhenRoomIsNotRecruiting_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.CloseRecruitment(QuestRoomCloseReason.Started, DateTimeOffset.UtcNow);

        var act = () => room.AddPlayer(ownerId, "Owner");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddPlayer_WhenBelowMinRequiredLevel_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = new QuestRoom(
            QuestRoomId.New(),
            ownerId,
            new QuestStageId(1),
            QuestRoomMode.Multi,
            joinPolicy: new QuestRoomJoinPolicy(minRequiredLevel: 10));

        var act = () => room.AddPlayer(new PlayerId(Guid.NewGuid()), "Guest", level: 9);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("参加可能レベルを満たしていないため、このルームには参加できません。");
    }

    [Fact]
    public void AddPlayer_WhenAllowedPlayerRestrictionDoesNotIncludePlayer_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var allowedPlayerId = new PlayerId(Guid.NewGuid());
        var room = new QuestRoom(
            QuestRoomId.New(),
            ownerId,
            new QuestStageId(1),
            QuestRoomMode.Multi,
            joinPolicy: new QuestRoomJoinPolicy(allowedPlayerIds: [allowedPlayerId]));

        var act = () => room.AddPlayer(new PlayerId(Guid.NewGuid()), "Guest", level: 10);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("このルームへの参加対象プレイヤーに含まれていません。");
    }

    [Fact]
    public void UpdateJoinPolicy_WhenRecruiting_UpdatesRestrictions()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var allowedPlayerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);

        room.UpdateJoinPolicy(12, [allowedPlayerId]);

        room.JoinPolicy.MinRequiredLevel.Should().Be(12);
        room.JoinPolicy.AllowedPlayerIds.Should().ContainSingle().Which.Should().Be(allowedPlayerId);
    }

    [Fact]
    public void CancelForOwnerRoomReplacement_WhenRecruiting_ClosesRoomAsCancelled()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        var at = DateTimeOffset.UtcNow;

        room.CancelForOwnerRoomReplacement(at);

        room.Status.Should().Be(QuestRoomStatus.Closed);
        room.CloseReason.Should().Be(QuestRoomCloseReason.Cancelled);
        room.ClosedAt.Should().Be(at);
    }

    [Fact]
    public void CancelByOwner_WhenRecruiting_ClosesRoomAsCancelled()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var at = DateTimeOffset.UtcNow;

        room.CancelByOwner(at);

        room.Status.Should().Be(QuestRoomStatus.Closed);
        room.CloseReason.Should().Be(QuestRoomCloseReason.Cancelled);
        room.ClosedAt.Should().Be(at);
    }

    [Fact]
    public void AssignPosition_WhenTargetPositionIsOccupied_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        var memberId = new PlayerId(Guid.NewGuid());
        room.AddPlayer(memberId, "Member");

        var owner = room.Participants.Single(x => x.PlayerId == ownerId);
        var member = room.Participants.Single(x => x.PlayerId == memberId);

        var act = () => room.AssignPosition(member.Id, owner.Position);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AssignPosition_WhenTargetPositionIsEmpty_MovesParticipant()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        var memberId = new PlayerId(Guid.NewGuid());
        room.AddPlayer(memberId, "Member");
        var member = room.Participants.Single(x => x.PlayerId == memberId);
        var target = new BattlePosition(BattleRow.Middle, BattleColumn.Left);

        room.AssignPosition(member.Id, target);

        room.Participants.Single(x => x.Id == member.Id).Position.Should().Be(target);
        room.Formation.OccupiedPositions.Should().Contain(target);
    }

    [Fact]
    public void AssignPosition_WhenTargetPositionIsSame_DoesNothing()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var owner = room.Participants.Single();
        var originalPositions = room.Formation.OccupiedPositions.ToArray();

        room.AssignPosition(owner.Id, owner.Position);

        room.Participants.Single().Position.Should().Be(owner.Position);
        room.Formation.OccupiedPositions.Should().BeEquivalentTo(originalPositions);
    }

    [Fact]
    public void AssignPosition_WhenParticipantAlreadyLeft_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        var memberId = new PlayerId(Guid.NewGuid());
        room.AddPlayer(memberId, "Member");
        var member = room.Participants.Single(x => x.PlayerId == memberId);
        room.RemoveParticipant(member.Id, DateTimeOffset.UtcNow);

        var act = () => room.AssignPosition(member.Id, new BattlePosition(BattleRow.Middle, BattleColumn.Left));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AssignPosition_WhenRoomIsNotRecruiting_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var owner = room.Participants.Single();
        room.CloseRecruitment(QuestRoomCloseReason.Started, DateTimeOffset.UtcNow);

        var act = () => room.AssignPosition(owner.Id, new BattlePosition(BattleRow.Middle, BattleColumn.Left));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDisconnected_WhenParticipantExists_UpdatesStatusAndLastSeenAt()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var owner = room.Participants.Single();
        var at = DateTimeOffset.UtcNow;

        room.MarkDisconnected(owner.Id, at);

        room.Participants.Single().Status.Should().Be(ParticipantStatus.Disconnected);
        room.Participants.Single().LastSeenAt.Should().Be(at);
    }

    [Fact]
    public void MarkDisconnected_WhenParticipantNotFound_ThrowsKeyNotFoundException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");

        var act = () => room.MarkDisconnected(QuestParticipantId.New(), DateTimeOffset.UtcNow);

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void MarkDisconnected_WhenRoomIsNotRecruiting_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var owner = room.Participants.Single();
        room.CloseRecruitment(QuestRoomCloseReason.Started, DateTimeOffset.UtcNow);

        var act = () => room.MarkDisconnected(owner.Id, DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkReconnected_WhenParticipantExists_UpdatesStatusAndLastSeenAt()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var owner = room.Participants.Single();
        room.MarkDisconnected(owner.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        var at = DateTimeOffset.UtcNow;

        room.MarkReconnected(owner.Id, at);

        room.Participants.Single().Status.Should().Be(ParticipantStatus.Joined);
        room.Participants.Single().LastSeenAt.Should().Be(at);
    }

    [Fact]
    public void MarkReconnected_WhenParticipantNotFound_ThrowsKeyNotFoundException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");

        var act = () => room.MarkReconnected(QuestParticipantId.New(), DateTimeOffset.UtcNow);

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void MarkReconnected_WhenRoomIsNotRecruiting_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var owner = room.Participants.Single();
        room.CloseRecruitment(QuestRoomCloseReason.Started, DateTimeOffset.UtcNow);

        var act = () => room.MarkReconnected(owner.Id, DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveParticipant_WhenNonOwnerExists_MarksLeftAndReleasesFormation()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        var memberId = new PlayerId(Guid.NewGuid());
        room.AddPlayer(memberId, "Member");
        var member = room.Participants.Single(x => x.PlayerId == memberId);
        var at = DateTimeOffset.UtcNow;

        room.RemoveParticipant(member.Id, at);

        room.Participants.Single(x => x.Id == member.Id).Status.Should().Be(ParticipantStatus.Left);
        room.Participants.Single(x => x.Id == member.Id).LeftAt.Should().Be(at);
        room.Formation.OccupiedPositions.Should().NotContain(member.Position);
    }

    [Fact]
    public void RemoveParticipant_WhenOwnerSpecified_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var owner = room.Participants.Single();

        var act = () => room.RemoveParticipant(owner.Id, DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveParticipant_WhenParticipantNotFound_ThrowsKeyNotFoundException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");

        var act = () => room.RemoveParticipant(QuestParticipantId.New(), DateTimeOffset.UtcNow);

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void RemoveParticipant_WhenRoomIsNotRecruiting_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        var memberId = new PlayerId(Guid.NewGuid());
        room.AddPlayer(memberId, "Member");
        var member = room.Participants.Single(x => x.PlayerId == memberId);
        room.CloseRecruitment(QuestRoomCloseReason.Started, DateTimeOffset.UtcNow);

        var act = () => room.RemoveParticipant(member.Id, DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CanStart_WhenModeIsMultiAndOnlyOneHumanPlayer_ReturnsFalse()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");

        room.CanStart().Should().BeFalse();
    }

    [Fact]
    public void CanStart_WhenModeIsMultiAndTwoHumanPlayersJoined_ReturnsTrue()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        room.AddPlayer(new PlayerId(Guid.NewGuid()), "Member");

        room.CanStart().Should().BeTrue();
    }

    [Fact]
    public void CanStart_WhenModeIsSoloAndOneHumanPlayerJoined_ReturnsTrue()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");

        room.CanStart().Should().BeTrue();
    }

    [Fact]
    public void CanStart_WhenRoomIsClosed_ReturnsFalse()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        room.CloseRecruitment(QuestRoomCloseReason.Started, DateTimeOffset.UtcNow);

        room.CanStart().Should().BeFalse();
    }

    [Fact]
    public void CanStart_WhenNoParticipants_ReturnsFalse()
    {
        var room = CreateRoom(new PlayerId(Guid.NewGuid()), QuestRoomMode.Solo);

        room.CanStart().Should().BeFalse();
    }

    [Fact]
    public void CanStart_WhenOwnerIsMissing_ReturnsFalse()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var participants = new[]
        {
            new QuestParticipant(
                QuestParticipantId.New(),
                ParticipantType.Player,
                "Member",
                new BattlePosition(BattleRow.Front, BattleColumn.Left),
                DateTimeOffset.UtcNow,
                isOwner: false,
                playerId: new PlayerId(Guid.NewGuid()))
        };
        var room = new QuestRoom(
            QuestRoomId.New(),
            ownerId,
            new QuestStageId(1),
            QuestRoomMode.Solo,
            new FormationLayout([participants[0].Position]),
            participants);

        room.CanStart().Should().BeFalse();
    }

    [Fact]
    public void AddNpcParticipants_WhenPreferredRowHasVacancy_AssignsNpcToPreferredRow()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.AddPlayer(ownerId, "Owner");
        var npcTemplate = new QuestNpcTemplate(
            new QuestNpcTemplateId(1),
            "Npc",
            Job.Warrior,
            BattleRow.Back,
            1,
            new Status(10, 1, 1, 1, 1, 1, 1),
            [new server.domain.move.MoveId(1)],
            NpcRole.FrontGuard);

        room.AddNpcParticipants([npcTemplate]);

        room.Participants.Single(x => x.Type == ParticipantType.Npc).Position.Row.Should().Be(BattleRow.Back);
    }

    [Fact]
    public void AddNpcParticipants_WhenPreferredRowIsFull_AssignsNpcToAnotherRow()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        room.AddPlayer(new PlayerId(Guid.NewGuid()), "Member");
        var npcTemplate1 = CreateNpcTemplate(1, BattleRow.Front);
        var npcTemplate2 = CreateNpcTemplate(2, BattleRow.Front);
        var npcTemplate3 = CreateNpcTemplate(3, BattleRow.Front);

        room.AddNpcParticipants([npcTemplate1, npcTemplate2, npcTemplate3]);

        var thirdNpc = room.Participants.Where(x => x.Type == ParticipantType.Npc).Last();
        thirdNpc.Position.Row.Should().NotBe(BattleRow.Front);
    }

    [Fact]
    public void AddNpcParticipants_WhenCapacityExceeded_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Multi);
        room.AddPlayer(ownerId, "Owner");
        for (var i = 0; i < 4; i++)
        {
            room.AddPlayer(new PlayerId(Guid.NewGuid()), $"Member{i}");
        }

        var firstNpc = CreateNpcTemplate(1, BattleRow.Back);
        var secondNpc = CreateNpcTemplate(2, BattleRow.Back);

        var act = () => room.AddNpcParticipants([firstNpc, secondNpc]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CloseRecruitment_WhenRecruiting_UpdatesStatusReasonAndClosedAt()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        var at = DateTimeOffset.UtcNow;

        room.CloseRecruitment(QuestRoomCloseReason.Started, at);

        room.Status.Should().Be(QuestRoomStatus.Closed);
        room.CloseReason.Should().Be(QuestRoomCloseReason.Started);
        room.ClosedAt.Should().Be(at);
    }

    [Fact]
    public void CloseRecruitment_WhenRoomIsNotRecruiting_ThrowsInvalidOperationException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);
        room.CloseRecruitment(QuestRoomCloseReason.Started, DateTimeOffset.UtcNow);

        var act = () => room.CloseRecruitment(QuestRoomCloseReason.Cancelled, DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SyncVersion_WhenVersionIsPositive_UpdatesVersion()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);

        room.SyncVersion(3);

        room.Version.Should().Be(3);
    }

    [Fact]
    public void SyncVersion_WhenVersionIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = CreateRoom(ownerId, QuestRoomMode.Solo);

        var act = () => room.SyncVersion(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static QuestRoom CreateRoom(PlayerId ownerId, QuestRoomMode mode)
    {
        return new QuestRoom(
            QuestRoomId.New(),
            ownerId,
            new QuestStageId(1),
            mode,
            new FormationLayout());
    }

    private static QuestNpcTemplate CreateNpcTemplate(int id, BattleRow preferredRow)
    {
        return new QuestNpcTemplate(
            new QuestNpcTemplateId(id),
            $"Npc{id}",
            Job.Warrior,
            preferredRow,
            1,
            new Status(10, 1, 1, 1, 1, 1, 1),
            [new server.domain.move.MoveId(1)],
            NpcRole.FrontGuard);
    }
}
