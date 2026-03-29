using server.shared.constants.chat;

namespace server.domain.chat;

public class ThreadBody(string value)
{
    public string Value { get; } = Validate(value);

    private static string Validate(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length is < 1 or > ThreadConstants.BodyMaxLength)
        {
            throw new ArgumentException($"本文は1文字から{ThreadConstants.BodyMaxLength}文字以内です.", nameof(value));
        }

        return normalized;
    }
}
