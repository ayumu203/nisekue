using server.shared.constants.player;

namespace server.domain.player;

public class Player(PlayerId id, string name, int level, int exp, Status status)
{
    public PlayerId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public int Level { get; private set; } = ValidateLevel(level);
    public int Exp { get; private set; } = exp;
    public Status Status { get; private set; } = status ?? throw new ArgumentNullException(nameof(status));

    public void UpdateName(string name)
    {
        Name = ValidateName(name);
    }

    public void UpdateStatus(Status status)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
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

    private static int ValidateLevel(int level)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "プレイヤーレベルは1以上である必要があります.");
        }

        return level;
    }

    public void GainExp(int exp)
    {
        if (exp < 0) exp = 0;
        Exp += exp;
    }
    public bool LevelUp()
    {
        bool flag = false;
        while (Exp >= Level * 10)
        {
            Exp -= Level * 10;
            Level++;
            flag = true;
        }
        return flag;
    }
}
