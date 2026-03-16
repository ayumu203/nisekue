using server.shared.constants.player;
using server.domain.move;

namespace server.domain.player;

public class Player(
    PlayerId id,
    string name,
    int level,
    int exp,
    int jobLevel,
    int jobExp,
    Status status,
    Job job = Job.Apprentice,
    string? imagePath = null,
    DateTimeOffset? questCooldownUntil = null,
    MoveSet? moveSet = null,
    IReadOnlySet<Job>? masteredJobs = null)
{
    public PlayerId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public string? ImagePath { get; private set; } = ValidateImagePath(imagePath);
    public DateTimeOffset? QuestCooldownUntil { get; private set; } = questCooldownUntil;
    public Job Job { get; private set; } = job;
    public int Level { get; private set; } = ValidateLevel(level);
    public int Exp { get; private set; } = exp;
    public int JobLevel { get; private set; } = ValidateLevel(jobLevel);
    public int JobExp { get; private set; } = jobExp;
    public Status Status { get; private set; } = status ?? throw new ArgumentNullException(nameof(status));
    public MoveSet MoveSet { get; private set; } = moveSet ?? new MoveSet();
    public IReadOnlySet<Job> MasteredJobs { get; } = new HashSet<Job>(masteredJobs is null ? Array.Empty<Job>() : masteredJobs);

    public void UpdateName(string name)
    {
        Name = ValidateName(name);
    }

    public void UpdateImagePath(string? imagePath)
    {
        ImagePath = ValidateImagePath(imagePath);
    }

    public IReadOnlyList<MoveId> ChangeJob(Job nextJob, JobProfile nextProfile, JobMoveLearningRule learningRule)
    {
        if (nextProfile.Job != nextJob)
        {
            throw new InvalidOperationException("転職先ジョブとジョブプロファイルが一致していません。");
        }

        if (learningRule.Job != nextJob)
        {
            throw new InvalidOperationException("転職先ジョブと技習得ルールが一致していません。");
        }

        if (!CanChangeJob(nextProfile))
        {
            throw new InvalidOperationException("転職条件を満たしていません。");
        }

        Job = nextJob;
        JobLevel = 1;
        JobExp = 0;
        return [];
    }

    public void UpdateStatus(Status status)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public void UpdateMoveSet(MoveSet moveSet)
    {
        MoveSet = moveSet ?? throw new ArgumentNullException(nameof(moveSet));
    }

    public void SetQuestCooldownUntil(DateTimeOffset? until)
    {
        QuestCooldownUntil = until;
    }

    private static string ValidateName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > PlayerConstants.NameMaxLength)
        {
            throw new ArgumentException($"プレイヤー名は1文字から{PlayerConstants.NameMaxLength}文字以内です.", nameof(name));
        }

        return normalized;
    }

    private static string? ValidateImagePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        var normalized = imagePath.Trim();
        if (normalized.Length > PlayerConstants.ImagePathMaxLength)
        {
            throw new ArgumentException($"画像パスは{PlayerConstants.ImagePathMaxLength}文字以内です.", nameof(imagePath));
        }

        return normalized;
    }

    private static int ValidateLevel(int level)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "レベルは1以上である必要があります。");
        }

        return level;
    }

    public void GainExp(int exp)
    {
        if (exp < 0) exp = 0;
        Exp += exp;
        JobExp += exp;
    }
    public LevelUpResult LevelUp(JobProfile jobProfile, JobMoveLearningRule learningRule)
    {
        if (jobProfile.Job != Job)
        {
            throw new InvalidOperationException("現在のジョブとジョブプロファイルが一致していません。");
        }

        if (learningRule.Job != Job)
        {
            throw new InvalidOperationException("現在のジョブと技習得ルールが一致していません。");
        }

        var growth = jobProfile.GrowthValue;
        var hasPlayerLeveledUp = false;
        while (Exp >= Level * 10)
        {
            Exp -= Level * 10;
            Level++;
            hasPlayerLeveledUp = true;
        }

        var hasJobLeveledUp = false;
        while (JobExp >= JobLevel * 10)
        {
            JobExp -= JobLevel * 10;
            JobLevel++;
            Status = new Status(
                maxHp: Status.MaxHp + growth.MaxHp,
                maxMp: Status.MaxMp + growth.MaxMp,
                strength: Status.Strength + growth.Strength,
                defense: Status.Defense + growth.Defense,
                intelligence: Status.Intelligence + growth.Intelligence,
                luck: Status.Luck + growth.Luck,
                speed: Status.Speed + growth.Speed);
            hasJobLeveledUp = true;
        }

        var hasMasteredCurrentJob = MarkCurrentJobAsMastered(jobProfile);
        var newlyLearnedMoveIds = SynchronizeLearnableMoves(jobProfile, learningRule);
        return new LevelUpResult(hasPlayerLeveledUp, hasJobLeveledUp, hasMasteredCurrentJob, newlyLearnedMoveIds);
    }

    private bool CanChangeJob(JobProfile nextProfile)
    {
        if (nextProfile.Job == Job)
        {
            return false;
        }

        if (nextProfile.Job == Job.Apprentice)
        {
            return false;
        }

        if (nextProfile.RequiredMasterJobs.Count == 0)
        {
            return Level >= 5;
        }

        return nextProfile.RequiredMasterJobs.All(job => MasteredJobs.Contains(job));
    }

    private bool MarkCurrentJobAsMastered(JobProfile jobProfile)
    {
        if (JobLevel < jobProfile.MasterLevel)
        {
            return false;
        }

        if (MasteredJobs.Contains(Job))
        {
            return false;
        }

        ((HashSet<Job>)MasteredJobs).Add(Job);
        return true;
    }

    private IReadOnlyList<MoveId> SynchronizeLearnableMoves(JobProfile jobProfile, JobMoveLearningRule learningRule)
    {
        var newlyLearnedMoveIds = new List<MoveId>();
        foreach (var moveId in learningRule.GetLearnableMoveIds(JobLevel, jobProfile.MasterLevel))
        {
            if (MoveSet.Contains(moveId))
            {
                continue;
            }

            MoveSet.AddLearnedMove(moveId);
            newlyLearnedMoveIds.Add(moveId);
        }

        return newlyLearnedMoveIds;
    }
}
