namespace server.domain.player;

public class Player(PlayerId id, string name, int level, int exp, BaseStatus status)
{
    public const int NameMaxLength = 20;

    public PlayerId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public int Level { get; private set; } = level;
    public int Exp { get; private set; } = exp;
    public BaseStatus Status {get; set;} = status;

    public void UpdateName(string name)
    {
        Name = ValidateName(name);
    }

    private static string ValidateName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > NameMaxLength)
        {
            throw new ArgumentException($"Player name length must be between 1 and {NameMaxLength} characters.", nameof(name));
        }

        return normalized;
    }
    public void GainExp(int exp)
    {
        Exp += exp;
    }
    public bool LevelUp()
    {
        if(Exp >= Level * 10)
        {
            Exp -= Level * 10;
            Level++;
            return true;
        }
        return false;
    }
}
