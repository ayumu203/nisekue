using server.shared.constants.player;
using server.domain.move;

namespace server.domain.player;

public class Player(
    PlayerId id,
    string name,
    int level,
    int exp,
    int jobLevel,
    int gold,
    Status status,
    Job job = Job.Apprentice,
    string? imagePath = null,
    DateTimeOffset? questCooldownUntil = null,
    DateTimeOffset? petBattleCooldownUntil = null,
    MoveSet? moveSet = null,
    IReadOnlySet<Job>? masteredJobs = null,
    int rebirthCount = 0,
    int expMultiplierFlags = 0,
    int mapUnlockFlags = 0,
    long roadmapUnlockFlags = 1,
    int endlessBestFloor = 0)
{
    private readonly HashSet<Job> masteredJobs = masteredJobs is null ? [] : new HashSet<Job>(masteredJobs);

    public PlayerId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public string? ImagePath { get; private set; } = ValidateImagePath(imagePath);
    public DateTimeOffset? QuestCooldownUntil { get; private set; } = questCooldownUntil;
    public DateTimeOffset? PetBattleCooldownUntil { get; private set; } = petBattleCooldownUntil;
    public Job Job { get; private set; } = job;
    public int RebirthCount { get; private set; } = ValidateNonNegative(rebirthCount, nameof(rebirthCount));
    public int Level { get; private set; } = ValidatePlayerLevel(level);
    public int Exp { get; private set; } = exp;
    public int JobLevel { get; private set; } = ValidateLevel(jobLevel);
    public int Gold { get; private set; } = ValidateNonNegative(gold, nameof(gold));
    public Status Status { get; private set; } = status ?? throw new ArgumentNullException(nameof(status));
    public MoveSet MoveSet { get; private set; } = moveSet ?? new MoveSet();
    public IReadOnlySet<Job> MasteredJobs => masteredJobs;
    public int ExpMultiplierFlags { get; private set; } = ValidateNonNegative(expMultiplierFlags, nameof(expMultiplierFlags));
    public int MapUnlockFlags { get; private set; } = ValidateNonNegative(mapUnlockFlags, nameof(mapUnlockFlags));
    public long RoadmapUnlockFlags { get; private set; } = ValidateNonNegativeLong(roadmapUnlockFlags, nameof(roadmapUnlockFlags));
    public int EndlessBestFloor { get; private set; } = ValidateNonNegative(endlessBestFloor, nameof(endlessBestFloor));

    public bool IsMaxLevel => Level >= PlayerConstants.MaxLevel;

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

    public void SetPetBattleCooldownUntil(DateTimeOffset? until)
    {
        PetBattleCooldownUntil = until;
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

    private static int ValidatePlayerLevel(int level)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "レベルは1以上である必要があります。");
        }

        if (level > PlayerConstants.MaxLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level), $"レベルは{PlayerConstants.MaxLevel}以下である必要があります。");
        }

        return level;
    }

    // レベル上限到達後は経験値を加算しない。職業レベルはプレイヤーレベルに連動するため同時に停止する。
    public void GainExp(int exp)
    {
        if (exp < 0) exp = 0;
        if (IsMaxLevel)
        {
            return;
        }

        Exp = ClampedAdd(Exp, exp);
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

    public void SetMapUnlockFlag(int flag)
    {
        if (!MapUnlockFlag.IsValidFlag(flag))
        {
            throw new ArgumentException("無効なマップ解放フラグです。", nameof(flag));
        }

        if ((MapUnlockFlags & flag) != 0)
        {
            throw new InvalidOperationException("すでにこのマップは解放済みです。");
        }

        MapUnlockFlags |= flag;
    }

    public bool HasMapUnlockFlag(int flag)
    {
        if (!MapUnlockFlag.IsValidFlag(flag))
        {
            throw new ArgumentException("無効なマップ解放フラグです。", nameof(flag));
        }

        return (MapUnlockFlags & flag) != 0;
    }

    public void ClearMapUnlockFlag(int flag)
    {
        if (!MapUnlockFlag.IsValidFlag(flag))
        {
            throw new ArgumentException("無効なマップ解放フラグです。", nameof(flag));
        }

        MapUnlockFlags &= ~flag;
    }

    /// <summary>エンドレス到達フロアでベスト記録を更新（より深い場合のみ）。</summary>
    public void UpdateEndlessBestFloor(int reachedFloor)
    {
        if (reachedFloor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reachedFloor), "0以上である必要があります。");
        }

        if (reachedFloor > EndlessBestFloor)
        {
            EndlessBestFloor = reachedFloor;
        }
    }

    public bool IsRoadmapUnlocked(Job job)
    {
        return (RoadmapUnlockFlags & (1L << ((int)job - 1))) != 0;
    }

    public void UnlockRoadmap(Job job)
    {
        RoadmapUnlockFlags |= (1L << ((int)job - 1));
    }

    private static long ValidateNonNegativeLong(long value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以上である必要があります。");
        }

        return value;
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

    public int RequiredExpForNextLevel()
    {
        if (IsMaxLevel)
        {
            return 0;
        }

        return Level * 10;
    }

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
        while (!IsMaxLevel && Exp >= RequiredExpForNextLevel())
        {
            Exp -= RequiredExpForNextLevel();
            var previousLevel = Level;
            Level++;
            JobLevel++;
            Status = new Status(
                maxHp: ClampedAdd(Status.MaxHp, CalculateGrowthIncrease(growth.MaxHp, previousLevel, Level)),
                maxMp: ClampedAdd(Status.MaxMp, CalculateGrowthIncrease(growth.MaxMp, previousLevel, Level)),
                strength: ClampedAdd(Status.Strength, CalculateGrowthIncrease(growth.Strength, previousLevel, Level)),
                defense: ClampedAdd(Status.Defense, CalculateGrowthIncrease(growth.Defense, previousLevel, Level)),
                intelligence: ClampedAdd(Status.Intelligence, CalculateGrowthIncrease(growth.Intelligence, previousLevel, Level)),
                luck: ClampedAdd(Status.Luck, CalculateGrowthIncrease(growth.Luck, previousLevel, Level)),
                speed: ClampedAdd(Status.Speed, CalculateGrowthIncrease(growth.Speed, previousLevel, Level)),
                accuracy: Status.Accuracy,
                evasion: Status.Evasion,
                criticalChance: Status.CriticalChance,
                damageReduction: Status.DamageReduction);
            hasPlayerLeveledUp = true;
        }

        if (IsMaxLevel)
        {
            Exp = 0;
        }

        // 職業レベルはプレイヤーレベルに連動するため、レベルアップの有無も一致する。
        var hasJobLeveledUp = hasPlayerLeveledUp;

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

    // 転生ごとの 1% 成長ボーナスを累積差分で配り、整数成長でも無駄にならないようにする。
    private int CalculateGrowthIncrease(int baseGrowth, int previousLevel, int currentLevel)
    {
        return ClampedAdd(baseGrowth, CalculateRebirthGrowthBonusDelta(baseGrowth, previousLevel, currentLevel));
    }

    private int CalculateRebirthGrowthBonusDelta(int baseGrowth, int previousLevel, int currentLevel)
    {
        if (baseGrowth <= 0 || RebirthCount <= 0)
        {
            return 0;
        }

        var previousBonus = CalculateCumulativeRebirthGrowthBonus(baseGrowth, previousLevel - 1);
        var currentBonus = CalculateCumulativeRebirthGrowthBonus(baseGrowth, currentLevel - 1);
        return currentBonus - previousBonus;
    }

    private int CalculateCumulativeRebirthGrowthBonus(int baseGrowth, int gainedLevels)
    {
        if (gainedLevels <= 0)
        {
            return 0;
        }

        return (int)Math.Min(
            (long)baseGrowth * gainedLevels * RebirthCount * PlayerConstants.RebirthGrowthBonusPercent / 100,
            int.MaxValue);
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
