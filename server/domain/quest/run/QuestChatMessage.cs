namespace server.domain.quest;

public class QuestChatMessage(
    QuestParticipantId senderParticipantId,
    string displayName,
    string? imagePath,
    string message,
    DateTimeOffset sentAt)
{
    public QuestParticipantId SenderParticipantId { get; } = senderParticipantId;
    public string DisplayName { get; } = ValidateRequired(displayName, nameof(displayName));
    public string? ImagePath { get; } = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
    public string Message { get; } = ValidateRequired(message, nameof(message));
    public DateTimeOffset SentAt { get; } = sentAt;

    private static string ValidateRequired(string value, string paramName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("値は必須です。", paramName);
        }

        return normalized;
    }
}
