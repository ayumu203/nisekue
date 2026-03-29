using server.shared.constants.chat;

namespace server.domain.chat;

public class ThreadTitle(string value)
{
    public string Value { get; } = Validate(value);

    private static string Validate(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length is < 1 or > ThreadConstants.TitleMaxLength)
        {
            throw new ArgumentException($"タイトルは1文字から{ThreadConstants.TitleMaxLength}文字以内です.", nameof(value));
        }

        return normalized;
    }
}
