namespace server.domain.player;

public class Equipment(
    EquipmentId id,
    string name,
    string flavorText,
    EquipmentType type,
    int maxDurability,
    int masteryCap,
    int synthesisGoldCost,
    EquipmentStatusBonus bonusValues,
    IReadOnlySet<Job> equippableJobs)
{
    private readonly HashSet<Job> _equippableJobs = equippableJobs is null ? [] : new HashSet<Job>(equippableJobs);

    public EquipmentId Id { get; } = id;
    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("装備名は必須です。", nameof(name)) : name.Trim();
    public string FlavorText { get; } = string.IsNullOrWhiteSpace(flavorText) ? string.Empty : flavorText.Trim();
    public EquipmentType Type { get; } = type;
    public int MaxDurability { get; } = maxDurability > 0 ? maxDurability : throw new ArgumentOutOfRangeException(nameof(maxDurability), "耐久値上限は1以上である必要があります。");
    public int MasteryCap { get; } = masteryCap >= 0 ? masteryCap : throw new ArgumentOutOfRangeException(nameof(masteryCap), "熟練度上限は0以上である必要があります。");
    public int SynthesisGoldCost { get; } = synthesisGoldCost >= 0 ? synthesisGoldCost : throw new ArgumentOutOfRangeException(nameof(synthesisGoldCost), "合成必要 Gold は0以上である必要があります。");
    public EquipmentStatusBonus BonusValues { get; } = bonusValues ?? throw new ArgumentNullException(nameof(bonusValues));
    public IReadOnlySet<Job> EquippableJobs => _equippableJobs;

    public bool CanEquip(Job job) =>
        _equippableJobs.Contains(job) ||
        JobHierarchy.GetAncestors(job).Any(ancestor => _equippableJobs.Contains(ancestor));
}
