using server.shared.constants.chat;

namespace server.domain.chat;

public class ChatText(string text)
{
    public string Text { get; } = ValidateMessage(text);

    private static string ValidateMessage(string text)
    {
        var normalized = (text ?? string.Empty).Trim();
        if (normalized.Length is < 1 or > ChatConstants.MessageMaxLength)
        {
            throw new ArgumentException($"メッセージは1文字から{ChatConstants.MessageMaxLength}文字以内です.", nameof(text));
        }

        return normalized;
    }
}
