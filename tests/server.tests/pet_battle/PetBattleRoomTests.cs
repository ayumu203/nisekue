using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.pet;
using server.domain.pet_battle;
using server.domain.pet_battle.enums;
using server.domain.player;
using Xunit;

namespace server.tests.pet_battle;

public class PetBattleRoomTests
{
    private static readonly PlayerId OwnerId = new(Guid.NewGuid());
    private static readonly PlayerId OpponentId = new(Guid.NewGuid());

    [Fact]
    public void Create_ReturnsWaitingForStartRoomWithNoSlots()
    {
        var now = DateTimeOffset.UtcNow;

        var room = PetBattleRoom.Create(OwnerId, OpponentId, now);

        room.OwnerPlayerId.Should().Be(OwnerId);
        room.OpponentPlayerId.Should().Be(OpponentId);
        room.Status.Should().Be(PetBattleRoomStatus.WaitingForStart);
        room.Slots.Should().BeEmpty();
        room.Version.Should().Be(1);
    }

    [Fact]
    public void AssignSlot_AddsNewSlot()
    {
        var room = CreateRoom();
        var petId = new PlayerPetId(Guid.NewGuid());

        room.AssignSlot(petId, BattleRow.Front, BattleColumn.Left);

        room.Slots.Should().ContainSingle(s => s.PetId == petId && s.Row == BattleRow.Front && s.Column == BattleColumn.Left);
    }

    [Fact]
    public void AssignSlot_WhenSamePetAssignedAgain_ReplacesOldSlot()
    {
        var room = CreateRoom();
        var petId = new PlayerPetId(Guid.NewGuid());
        room.AssignSlot(petId, BattleRow.Front, BattleColumn.Left);

        room.AssignSlot(petId, BattleRow.Middle, BattleColumn.Right);

        room.Slots.Should().ContainSingle(s => s.PetId == petId);
        room.Slots[0].Row.Should().Be(BattleRow.Middle);
    }

    [Fact]
    public void AssignSlot_WhenPositionAlreadyOccupied_DisplacesOldPet()
    {
        var room = CreateRoom();
        var pet1 = new PlayerPetId(Guid.NewGuid());
        var pet2 = new PlayerPetId(Guid.NewGuid());
        room.AssignSlot(pet1, BattleRow.Front, BattleColumn.Left);

        room.AssignSlot(pet2, BattleRow.Front, BattleColumn.Left);

        room.Slots.Should().ContainSingle(s => s.PetId == pet2);
        room.Slots.Should().NotContain(s => s.PetId == pet1);
    }

    [Fact]
    public void RemoveSlot_RemovesExistingPet()
    {
        var room = CreateRoom();
        var petId = new PlayerPetId(Guid.NewGuid());
        room.AssignSlot(petId, BattleRow.Front, BattleColumn.Left);

        room.RemoveSlot(petId);

        room.Slots.Should().BeEmpty();
    }

    [Fact]
    public void RemoveSlot_WhenPetNotFound_DoesNotThrow()
    {
        var room = CreateRoom();

        var act = () => room.RemoveSlot(new PlayerPetId(Guid.NewGuid()));

        act.Should().NotThrow();
    }

    [Fact]
    public void CanStart_WhenNoSlots_ReturnsFalse()
    {
        var room = CreateRoom();

        room.CanStart().Should().BeFalse();
    }

    [Fact]
    public void CanStart_WhenSlotAssigned_ReturnsTrue()
    {
        var room = CreateRoom();
        room.AssignSlot(new PlayerPetId(Guid.NewGuid()), BattleRow.Front, BattleColumn.Left);

        room.CanStart().Should().BeTrue();
    }

    [Fact]
    public void CloseForStart_ChangesStatusToClosedAndIncrementsVersion()
    {
        var room = CreateRoom();
        room.AssignSlot(new PlayerPetId(Guid.NewGuid()), BattleRow.Front, BattleColumn.Left);
        var now = DateTimeOffset.UtcNow;

        room.CloseForStart(now);

        room.Status.Should().Be(PetBattleRoomStatus.Closed);
        room.CloseReason.Should().Be(PetBattleRoomCloseReason.Started);
        room.ClosedAt.Should().Be(now);
        room.Version.Should().Be(2);
    }

    [Fact]
    public void Cancel_ChangesStatusToClosedWithCancelledReason()
    {
        var room = CreateRoom();
        var now = DateTimeOffset.UtcNow;

        room.Cancel(now);

        room.Status.Should().Be(PetBattleRoomStatus.Closed);
        room.CloseReason.Should().Be(PetBattleRoomCloseReason.Cancelled);
        room.ClosedAt.Should().Be(now);
    }

    [Fact]
    public void AssignSlot_WhenRoomClosed_ThrowsInvalidOperationException()
    {
        var room = CreateRoom();
        room.Cancel(DateTimeOffset.UtcNow);

        var act = () => room.AssignSlot(new PlayerPetId(Guid.NewGuid()), BattleRow.Front, BattleColumn.Left);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveSlot_WhenRoomClosed_ThrowsInvalidOperationException()
    {
        var room = CreateRoom();
        var petId = new PlayerPetId(Guid.NewGuid());
        room.AssignSlot(petId, BattleRow.Front, BattleColumn.Left);
        room.Cancel(DateTimeOffset.UtcNow);

        var act = () => room.RemoveSlot(petId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CanStart_WhenStatusClosedWithSlots_ReturnsFalse()
    {
        var room = CreateRoom();
        room.AssignSlot(new PlayerPetId(Guid.NewGuid()), BattleRow.Front, BattleColumn.Left);
        room.CloseForStart(DateTimeOffset.UtcNow);

        room.CanStart().Should().BeFalse();
    }

    [Fact]
    public void CloseForStart_WhenAlreadyClosed_ThrowsInvalidOperationException()
    {
        var room = CreateRoom();
        room.Cancel(DateTimeOffset.UtcNow);

        var act = () => room.CloseForStart(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenAlreadyClosed_ThrowsInvalidOperationException()
    {
        var room = CreateRoom();
        room.Cancel(DateTimeOffset.UtcNow);

        var act = () => room.Cancel(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    private static PetBattleRoom CreateRoom() =>
        PetBattleRoom.Create(OwnerId, OpponentId, DateTimeOffset.UtcNow);
}
