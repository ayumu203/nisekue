namespace server.domain.player;

public class Item(
    ItemId id,
    string name,
    string flavorText,
    int maxStack,
    ItemEffectType effectType,
    StatusBonus? statusBonus = null,
    StatusBonusPercent? statusBonusPercent = null,
    Job? changeJobTo = null,
    int? requiredLevel = null,
    IReadOnlySet<Job>? requiredMasterJobs = null,
    decimal? expMultiplier = null)
{
    private readonly HashSet<Job> _requiredMasterJobs = requiredMasterJobs is null
        ? []
        : [.. requiredMasterJobs];

    public ItemId Id { get; } = id;
    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("アイテム名は必須です。", nameof(name)) : name.Trim();
    public string FlavorText { get; } = string.IsNullOrWhiteSpace(flavorText) ? string.Empty : flavorText.Trim();
    public int MaxStack { get; } = maxStack > 0 ? maxStack : throw new ArgumentOutOfRangeException(nameof(maxStack), "スタック上限は1以上である必要があります。");
    public ItemEffectType EffectType { get; } = effectType;
    public StatusBonus? StatusBonus { get; } = statusBonus;
    public StatusBonusPercent? StatusBonusPercent { get; } = statusBonusPercent;
    public Job? ChangeJobTo { get; } = changeJobTo;
    public int? RequiredLevel { get; } = requiredLevel;
    public IReadOnlySet<Job> RequiredMasterJobs => _requiredMasterJobs;
    public decimal? ExpMultiplier { get; } = expMultiplier;

    public bool CanUse(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (RequiredLevel is not null && player.Level < RequiredLevel.Value)
        {
            return false;
        }

        return _requiredMasterJobs.All(player.MasteredJobs.Contains);
    }
}
