namespace server.domain.player;

public class Player
{
    public const int NameMaxLength = 20;

    public Player(PlayerId id, string name)
    {
        Id = id;
        Name = ValidateName(name);
    }

    public PlayerId Id { get; }
    public string Name { get; private set; }

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
}
