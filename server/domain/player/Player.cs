using server.shared.constants.player;

namespace server.domain.player;

public class Player(
    PlayerId id,
    string name,
    int level,
    int exp,
    Status status,
    Job job = Job.Apprentice,
    string? imagePath = null,
    MoveSet? moveSet = null)
{
    public PlayerId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public string? ImagePath { get; private set; } = ValidateImagePath(imagePath);
    public Job Job { get; private set; } = job;
    public int Level { get; private set; } = ValidateLevel(level);
    public int Exp { get; private set; } = exp;
    public Status Status { get; private set; } = status ?? throw new ArgumentNullException(nameof(status));
    public MoveSet MoveSet { get; private set; } = moveSet ?? new MoveSet();

    public void UpdateName(string name)
    {
        Name = ValidateName(name);
    }

    public void UpdateImagePath(string? imagePath)
    {
        ImagePath = ValidateImagePath(imagePath);
    }

    public void UpdateJob(Job job)
    {
        Job = job;
    }

    public void UpdateStatus(Status status)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public void UpdateMoveSet(MoveSet moveSet)
    {
        MoveSet = moveSet ?? throw new ArgumentNullException(nameof(moveSet));
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
            throw new ArgumentOutOfRangeException(nameof(level), "プレイヤーレベルは1以上である必要があります。");
        }

        return level;
    }

    public void GainExp(int exp)
    {
        if (exp < 0) exp = 0;
        Exp += exp;
    }
    public bool LevelUp(IGrowthValueRepository growthValueRepository)
    {
        var growth = growthValueRepository.GetByJob(Job);
        bool flag = false;
        while (Exp >= Level * 10)
        {
            Exp -= Level * 10;
            Level++;
            Status = new Status(
                maxHp: Status.MaxHp + growth.MaxHp,
                maxMp: Status.MaxMp + growth.MaxMp,
                strength: Status.Strength + growth.Strength,
                defense: Status.Defense + growth.Defense,
                intelligence: Status.Intelligence + growth.Intelligence,
                luck: Status.Luck + growth.Luck,
                speed: Status.Speed + growth.Speed);
            flag = true;
        }
        return flag;
    }
}
