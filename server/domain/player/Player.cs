using server.domain.shared;

namespace server.domain.player;

public class Player(PlayerId id, string name, int level, int exp, BaseStatus status)
{
    public PlayerId Id { get; } = id;
    public string Name { get; private set; } = ValidateName(name);
    public int Level { get; private set; } = level;
    public int Exp { get; private set; } = exp;
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
        if (normalized.Length is < 1 or > DomainConstraints.Names.PlayerMaxLength)
        {
            throw new ArgumentException($"プレイヤー名は1文字から{DomainConstraints.Names.PlayerMaxLength}文字以内です.", nameof(name));
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
