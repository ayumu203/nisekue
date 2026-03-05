namespace server.domain.chat;

public class ChatText(string text)
{
    public const int MessageMaxLength = 200;

    public string Text { get; } = ValidateMessage(text);

    private static string ValidateMessage(string text)
    {
        var normalized = (text ?? string.Empty).Trim();
        if (normalized.Length is < 1 or > MessageMaxLength)
        {
            throw new ArgumentException($"メッセージは1文字から{MessageMaxLength}文字以内です.", nameof(text));
        }

        return normalized;
    }
}
