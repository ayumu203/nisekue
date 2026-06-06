namespace server.domain.player;

public class PlayerEquipment(
    PlayerEquipmentId id,
    PlayerId playerId,
    EquipmentId equipmentId,
    EquipmentType type,
    EquipmentStatus status,
    int durability,
    int mastery,
    int plusValue,
    DateTimeOffset acquiredAt,
    DateTimeOffset updatedAt)
{
    public PlayerEquipmentId Id { get; } = id;
    public PlayerId PlayerId { get; private set; } = playerId;
    public EquipmentId EquipmentId { get; } = equipmentId;
    public EquipmentType Type { get; } = type;
    public EquipmentStatus Status { get; private set; } = status;
    public int Durability { get; private set; } = ValidateNonNegative(durability, nameof(durability));
    public int Mastery { get; private set; } = ValidateNonNegative(mastery, nameof(mastery));
    public int PlusValue { get; private set; } = ValidateNonNegative(plusValue, nameof(plusValue));
    public DateTimeOffset AcquiredAt { get; } = acquiredAt;
    public DateTimeOffset UpdatedAt { get; private set; } = updatedAt;
    public bool IsBroken => Status == EquipmentStatus.Broken || Durability <= 0;
    public const int MaxPlusValue = 100;

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

    public void Synthesize(PlayerEquipment source, int synthesisGoldCost, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.EquipmentId != EquipmentId)
        {
            throw new InvalidOperationException("同一装備でしか合成はできません。");
        }

        if (source.Id == Id)
        {
            throw new InvalidOperationException("同じ装備個体は合成できません。");
        }

        if (source.IsBroken)
        {
            throw new InvalidOperationException("破損した装備は合成素材にできません。");
        }

        if (IsBroken)
        {
            throw new InvalidOperationException("破損した装備は合成対象にできません。");
        }

        if (PlusValue >= MaxPlusValue)
        {
            throw new InvalidOperationException("プラス値が上限に達しているため、これ以上合成できません。");
        }

        if (synthesisGoldCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(synthesisGoldCost), "合成必要Goldは0以上である必要があります。");
        }

        PlusValue += 1;
        UpdatedAt = now;
    }

    [Obsolete("耐久値は使用しなくなったため、このメソッドは呼び出されません。")]
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

    [Obsolete("耐久値は使用しなくなったため、このメソッドは呼び出されません。")]
    public void RepairDurability(int value, int maxDurability, DateTimeOffset now)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "耐久値の回復量に負数は指定できません。");
        }

        if (maxDurability < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDurability), "耐久値上限は1以上である必要があります。");
        }

        if (IsBroken)
        {
            throw new InvalidOperationException("破損した装備は修復対象にできません。");
        }

        Durability = Math.Min(maxDurability, Durability + value);
        UpdatedAt = now;
    }

    public void TransferOwnership(PlayerId playerId, DateTimeOffset now)
    {
        PlayerId = playerId;
        if (Status != EquipmentStatus.Broken)
        {
            Status = EquipmentStatus.Inventory;
        }

        UpdatedAt = now;
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

    public void IncreaseMastery(int value, int masteryCap, DateTimeOffset now)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "熟練度の増加量に負数は指定できません。");
        }

        if (masteryCap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(masteryCap), "熟練度上限は0以上である必要があります。");
        }

        Mastery = Math.Min(masteryCap, Mastery + value);
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
