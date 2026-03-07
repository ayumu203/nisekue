namespace server.domain.player;

public class Player(PlayerId id, string name, int level, int exp, Status status)
{
    public const int NameMaxLength = 20;

    public PlayerId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public int Level { get; private set; } = level;
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
        if (normalized.Length is < 1 or > NameMaxLength)
        {
            throw new ArgumentException($"プレイヤー名は1文字から{NameMaxLength}文字以内です.", nameof(name));
        }

        return normalized;
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
