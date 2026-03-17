namespace server.domain.player;

public sealed class JobProfile(
    Job job,
    string description,
    int masterLevel,
    IReadOnlyList<Job> requiredMasterJobs,
    GrowthValue growthValue)
{
    public Job Job { get; } = job;
    public string Description { get; } = ValidateDescription(description);
    public int MasterLevel { get; } = ValidateMasterLevel(masterLevel);
    public IReadOnlyList<Job> RequiredMasterJobs { get; } = requiredMasterJobs ?? throw new ArgumentNullException(nameof(requiredMasterJobs));
    public GrowthValue GrowthValue { get; } = growthValue ?? throw new ArgumentNullException(nameof(growthValue));

    private static string ValidateDescription(string description)
    {
        var normalized = description?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("職業説明は1文字以上である必要があります。", nameof(description));
        }

        return normalized;
    }

    private static int ValidateMasterLevel(int masterLevel)
    {
        if (masterLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(masterLevel), "マスターレベルは1以上である必要があります。");
        }

        return masterLevel;
    }
}
