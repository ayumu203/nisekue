using server.domain.player;
using server.shared.constants.training;

namespace server.domain.training;

public class TrainingEnemy(TrainingEnemyId id, string name, string imagePath, int level, BaseStatus status)
{
    public TrainingEnemyId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public string ImagePath { get; private set; } = ValidateImagePath(imagePath);
    public int Level { get; } = ValidateLevel(level);
    public BaseStatus Status { get; private set; } = status ?? throw new ArgumentNullException(nameof(status));

    public void UpdateName(string name)
    {
        Name = ValidateName(name);
    }

    public void UpdateStatus(BaseStatus status)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    private static string ValidateName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > TrainingConstants.Constraints.EnemyNameMaxLength)
        {
            throw new ArgumentException($"敵名は1文字から{TrainingConstants.Constraints.EnemyNameMaxLength}文字以内です.", nameof(name));
        }

        return normalized;
    }

    private static string ValidateImagePath(string imagePath)
    {
        var normalized = imagePath?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > TrainingConstants.Constraints.EnemyImagePathMaxLength)
        {
            throw new ArgumentException($"画像パスは1文字から{TrainingConstants.Constraints.EnemyImagePathMaxLength}文字以内です.", nameof(imagePath));
        }

        return normalized;
    }

    private static int ValidateLevel(int level)
    {
        if (level <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "レベルに0以下の値は代入できないです.");
        }

        return level;
    }
}
