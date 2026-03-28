namespace server.domain.player;

public class PlayerEquipment(
    PlayerEquipmentId id,
    PlayerId playerId,
    EquipmentId equipmentId,
    EquipmentType type,
    EquipmentStatus status,
    int durability,
    int mastery,
    DateTimeOffset acquiredAt,
    DateTimeOffset updatedAt)
{
    public PlayerEquipmentId Id { get; } = id;
    public PlayerId PlayerId { get; } = playerId;
    public EquipmentId EquipmentId { get; } = equipmentId;
    public EquipmentType Type { get; } = type;
    public EquipmentStatus Status { get; private set; } = status;
    public int Durability { get; private set; } = ValidateNonNegative(durability, nameof(durability));
    public int Mastery { get; private set; } = ValidateNonNegative(mastery, nameof(mastery));
    public DateTimeOffset AcquiredAt { get; } = acquiredAt;
    public DateTimeOffset UpdatedAt { get; private set; } = updatedAt;
    public bool IsBroken => Status == EquipmentStatus.Broken || Durability <= 0;

    public void Equip(Equipment equipment, Job playerJob, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        if (equipment.Id != EquipmentId)
        {
            throw new InvalidOperationException("装備マスタと装備個体が一致していません。");
        }

        if (equipment.Type != Type)
        {
            throw new InvalidOperationException("装備種別が一致していません。");
        }

        if (IsBroken)
        {
            throw new InvalidOperationException("破損した装備は装備できません。");
        }

        if (!equipment.CanEquip(playerJob))
        {
            throw new InvalidOperationException("現在の職業ではこの装備を装備できません。");
        }

        Status = EquipmentStatus.Equipped;
        UpdatedAt = now;
    }

    public void Unequip(DateTimeOffset now)
    {
        if (Status == EquipmentStatus.Broken)
        {
            return;
        }

        Status = EquipmentStatus.Inventory;
        UpdatedAt = now;
    }

    public void ConsumeDurability(int value, DateTimeOffset now)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "耐久値の消費量に負数は指定できません。");
        }

        if (value == 0 || Status != EquipmentStatus.Equipped)
        {
            return;
        }

        Durability = Math.Max(0, Durability - value);
        UpdatedAt = now;
        if (Durability == 0)
        {
            Status = EquipmentStatus.Broken;
        }
    }

    public void IncreaseMastery(int value, DateTimeOffset now)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "熟練度の増加量に負数は指定できません。");
        }

        Mastery += value;
        UpdatedAt = now;
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以上である必要があります。");
        }

        return value;
    }
}
