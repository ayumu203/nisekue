namespace server.domain.player;

public class Equipment(
    EquipmentId id,
    string name,
    EquipmentType type,
    int maxDurability,
    EquipmentStatusBonus bonusValues,
    IReadOnlySet<Job> equippableJobs)
{
    private readonly HashSet<Job> _equippableJobs = equippableJobs is null ? [] : new HashSet<Job>(equippableJobs);

    public EquipmentId Id { get; } = id;
    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("装備名は必須です。", nameof(name)) : name.Trim();
    public EquipmentType Type { get; } = type;
    public int MaxDurability { get; } = maxDurability > 0 ? maxDurability : throw new ArgumentOutOfRangeException(nameof(maxDurability), "耐久値上限は1以上である必要があります。");
    public EquipmentStatusBonus BonusValues { get; } = bonusValues ?? throw new ArgumentNullException(nameof(bonusValues));
    public IReadOnlySet<Job> EquippableJobs => _equippableJobs;

    public bool CanEquip(Job job) => _equippableJobs.Contains(job);
}
