namespace server.domain.player;

public sealed class JobProfile(
    Job job,
    int masterLevel,
    IReadOnlyList<Job> requiredMasterJobs,
    GrowthValue growthValue)
{
    public Job Job { get; } = job;
    public int MasterLevel { get; } = ValidateMasterLevel(masterLevel);
    public IReadOnlyList<Job> RequiredMasterJobs { get; } = requiredMasterJobs ?? throw new ArgumentNullException(nameof(requiredMasterJobs));
    public GrowthValue GrowthValue { get; } = growthValue ?? throw new ArgumentNullException(nameof(growthValue));

    private static int ValidateMasterLevel(int masterLevel)
    {
        if (masterLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(masterLevel), "マスターレベルは1以上である必要があります。");
        }

        return masterLevel;
    }
}
