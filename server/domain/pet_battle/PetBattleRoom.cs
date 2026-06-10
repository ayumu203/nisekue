using server.domain.battle;
using server.domain.battle.enums;
using server.domain.pet;
using server.domain.pet_battle.enums;
using server.domain.player;

namespace server.domain.pet_battle;

public class PetBattleRoom(
    PetBattleRoomId id,
    PlayerId ownerPlayerId,
    PlayerId opponentPlayerId,
    PetBattleRoomStatus status,
    int version,
    IEnumerable<PetBattleRoomSlot>? slots = null,
    PetBattleRoomCloseReason? closeReason = null,
    DateTimeOffset? closedAt = null,
    DateTimeOffset? createdAt = null)
{
    private readonly List<PetBattleRoomSlot> slots = slots?.ToList() ?? [];

    public PetBattleRoomId Id { get; } = id;
    public PlayerId OwnerPlayerId { get; } = ownerPlayerId;
    public PlayerId OpponentPlayerId { get; } = opponentPlayerId;
    public PetBattleRoomStatus Status { get; private set; } = status;
    public int Version { get; private set; } = version;
    public IReadOnlyList<PetBattleRoomSlot> Slots => slots;
    public PetBattleRoomCloseReason? CloseReason { get; private set; } = closeReason;
    public DateTimeOffset? ClosedAt { get; private set; } = closedAt;
    public DateTimeOffset CreatedAt { get; } = createdAt ?? DateTimeOffset.UtcNow;

    public static PetBattleRoom Create(
        PlayerId ownerPlayerId,
        PlayerId opponentPlayerId,
        DateTimeOffset now) =>
        new(
            PetBattleRoomId.NewId(),
            ownerPlayerId,
            opponentPlayerId,
            PetBattleRoomStatus.WaitingForStart,
            version: 1,
            createdAt: now);

    public void AssignSlot(PlayerPetId petId, BattleRow row, BattleColumn column)
    {
        EnsureWaitingForStart();

        var existing = slots.FirstOrDefault(s => s.PetId == petId);
        if (existing is not null)
        {
            slots.Remove(existing);
        }

        var occupied = slots.FirstOrDefault(s => s.Row == row && s.Column == column);
        if (occupied is not null)
        {
            slots.Remove(occupied);
        }

        slots.Add(new PetBattleRoomSlot(petId, row, column));
    }

    public void RemoveSlot(PlayerPetId petId)
    {
        EnsureWaitingForStart();
        var slot = slots.FirstOrDefault(s => s.PetId == petId);
        if (slot is not null)
        {
            slots.Remove(slot);
        }
    }

    public bool CanStart() => Status == PetBattleRoomStatus.WaitingForStart && slots.Count > 0;

    public void CloseForStart(DateTimeOffset now)
    {
        EnsureWaitingForStart();
        Status = PetBattleRoomStatus.Closed;
        CloseReason = PetBattleRoomCloseReason.Started;
        ClosedAt = now;
        Version++;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureWaitingForStart();
        Status = PetBattleRoomStatus.Closed;
        CloseReason = PetBattleRoomCloseReason.Cancelled;
        ClosedAt = now;
        Version++;
    }

    private void EnsureWaitingForStart()
    {
        if (Status != PetBattleRoomStatus.WaitingForStart)
        {
            throw new InvalidOperationException("対戦ルームはすでに終了しています。");
        }
    }
}
