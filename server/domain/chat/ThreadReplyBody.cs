using server.shared.constants.chat;

namespace server.domain.chat;

public class ThreadReplyBody(string value)
{
    public string Value { get; } = Validate(value);

    private static string Validate(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length is < 1 or > ThreadConstants.ReplyBodyMaxLength)
        {
            throw new ArgumentException($"返信は1文字から{ThreadConstants.ReplyBodyMaxLength}文字以内です.", nameof(value));
        }

        return normalized;
    }
}
