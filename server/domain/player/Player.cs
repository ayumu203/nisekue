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
    int gold,
    Status status,
    Job job = Job.Apprentice,
    string? imagePath = null,
    DateTimeOffset? questCooldownUntil = null,
    MoveSet? moveSet = null,
    IReadOnlySet<Job>? masteredJobs = null,
    int rebirthCount = 0,
    int expMultiplierFlags = 0)
{
    private readonly HashSet<Job> masteredJobs = masteredJobs is null ? [] : new HashSet<Job>(masteredJobs);

    public PlayerId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public string? ImagePath { get; private set; } = ValidateImagePath(imagePath);
    public DateTimeOffset? QuestCooldownUntil { get; private set; } = questCooldownUntil;
    public Job Job { get; private set; } = job;
    public int RebirthCount { get; private set; } = ValidateNonNegative(rebirthCount, nameof(rebirthCount));
    public int Level { get; private set; } = ValidateLevel(level);
    public int Exp { get; private set; } = exp;
    public int JobLevel { get; private set; } = ValidateLevel(jobLevel);
    public int JobExp { get; private set; } = jobExp;
    public int Gold { get; private set; } = ValidateNonNegative(gold, nameof(gold));
    public Status Status { get; private set; } = status ?? throw new ArgumentNullException(nameof(status));
    public MoveSet MoveSet { get; private set; } = moveSet ?? new MoveSet();
    public IReadOnlySet<Job> MasteredJobs => masteredJobs;
    public int ExpMultiplierFlags { get; private set; } = ValidateNonNegative(expMultiplierFlags, nameof(expMultiplierFlags));

    public void UpdateName(string name)
    {
        Name = ValidateName(name);
    }

    public void UpdateImagePath(string? imagePath)
    {
        ImagePath = ValidateImagePath(imagePath);
    }

    public IReadOnlyList<MoveId> ChangeJob(
        Job nextJob,
        JobProfile nextProfile,
        JobMoveLearningRule learningRule,
        bool ignoreRequirements = false)
    {
        if (nextProfile.Job != nextJob)
        {
            throw new InvalidOperationException("転職先ジョブとジョブプロファイルが一致していません。");
        }

        if (learningRule.Job != nextJob)
        {
            throw new InvalidOperationException("転職先ジョブとスキル習得ルールが一致していません。");
        }

        if (nextProfile.Job == Job)
        {
            throw new InvalidOperationException("転職条件を満たしていません。");
        }

        if (nextProfile.Job == Job.Apprentice)
        {
            throw new InvalidOperationException("転職条件を満たしていません。");
        }

        if (!ignoreRequirements && !CanChangeJob(nextProfile))
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
        Exp = ClampedAdd(Exp, exp);
        JobExp = ClampedAdd(JobExp, exp);
    }

    public void SetExpMultiplierFlag(int flag)
    {
        if (!ExpMultiplierFlag.IsValidFlag(flag))
        {
            throw new ArgumentException("無効な経験値倍率フラグです。", nameof(flag));
        }

        if (ExpMultiplierFlags != 0)
        {
            throw new InvalidOperationException("すでに経験値倍率が設定されています。");
        }

        ExpMultiplierFlags = flag;
    }

    public bool HasAnyExpMultiplierFlag()
    {
        return ExpMultiplierFlags != 0;
    }

    public void ClearExpMultiplierFlags()
    {
        ExpMultiplierFlags = 0;
    }

    public void GainGold(int gold)
    {
        Gold = ClampedAdd(Gold, ValidateNonNegative(gold, nameof(gold)));
    }

    public void SpendGold(int gold)
    {
        var validated = ValidateNonNegative(gold, nameof(gold));
        if (Gold < validated)
        {
            throw new InvalidOperationException("所持 Gold が不足しています。");
        }

        Gold -= validated;
    }

    public int RequiredExpForNextLevel() => Level * 10;

    public int RequiredJobExpForNextLevel() => JobLevel * 10;

    public LevelUpResult LevelUp(JobProfile jobProfile, JobMoveLearningRule learningRule)
    {
        if (jobProfile.Job != Job)
        {
            throw new InvalidOperationException("現在のジョブとジョブプロファイルが一致していません。");
        }

        if (learningRule.Job != Job)
        {
            throw new InvalidOperationException("現在のジョブとスキル習得ルールが一致していません。");
        }

        var growth = jobProfile.GrowthValue;
        var hasPlayerLeveledUp = false;
        while (Exp >= RequiredExpForNextLevel())
        {
            Exp -= RequiredExpForNextLevel();
            Level++;
            Status = new Status(
                maxHp: ClampedAdd(Status.MaxHp, growth.MaxHp),
                maxMp: ClampedAdd(Status.MaxMp, growth.MaxMp),
                strength: ClampedAdd(Status.Strength, growth.Strength),
                defense: ClampedAdd(Status.Defense, growth.Defense),
                intelligence: ClampedAdd(Status.Intelligence, growth.Intelligence),
                luck: ClampedAdd(Status.Luck, growth.Luck),
                speed: ClampedAdd(Status.Speed, growth.Speed),
                accuracy: Status.Accuracy,
                evasion: Status.Evasion,
                criticalChance: Status.CriticalChance,
                damageReduction: Status.DamageReduction);
            hasPlayerLeveledUp = true;
        }

        var hasJobLeveledUp = false;
        while (JobExp >= RequiredJobExpForNextLevel())
        {
            JobExp -= RequiredJobExpForNextLevel();
            JobLevel++;
            hasJobLeveledUp = true;
        }

        var hasMasteredCurrentJob = MarkCurrentJobAsMastered(jobProfile);
        var newlyLearnedMoveIds = SynchronizeLearnableMoves(jobProfile, learningRule);
        return new LevelUpResult(hasPlayerLeveledUp, hasJobLeveledUp, hasMasteredCurrentJob, newlyLearnedMoveIds);
    }

    public void Rebirth(Status inheritedStatus)
    {
        ArgumentNullException.ThrowIfNull(inheritedStatus);

        if (Level < PlayerConstants.RebirthRequiredLevel)
        {
            throw new InvalidOperationException($"転生にはレベル{PlayerConstants.RebirthRequiredLevel}以上が必要です。");
        }

        if (Gold < PlayerConstants.RebirthGoldCost)
        {
            throw new InvalidOperationException($"転生には{PlayerConstants.RebirthGoldCost} Goldが必要です。");
        }

        Gold -= PlayerConstants.RebirthGoldCost;
        RebirthCount++;
        Level = 1;
        Exp = 0;
        JobLevel = 1;
        JobExp = 0;
        Status = inheritedStatus;
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

        masteredJobs.Add(Job);
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

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以上である必要があります。");
        }

        return value;
    }

    private static int ClampedAdd(int a, int b) => (int)Math.Min((long)a + b, int.MaxValue);
}
