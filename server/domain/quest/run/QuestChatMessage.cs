namespace server.domain.quest;

public class QuestChatMessage(
    int turnNo,
    QuestParticipantId senderParticipantId,
    string displayName,
    string? imagePath,
    string message,
    DateTimeOffset sentAt)
{
    public int TurnNo { get; } = ValidateTurnNo(turnNo);
    public QuestParticipantId SenderParticipantId { get; } = senderParticipantId;
    public string DisplayName { get; } = ValidateRequired(displayName, nameof(displayName));
    public string? ImagePath { get; } = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
    public string Message { get; } = ValidateRequired(message, nameof(message));
    public DateTimeOffset SentAt { get; } = sentAt;

    private static int ValidateTurnNo(int turnNo)
    {
        if (turnNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnNo), "ターン番号は1以上である必要があります。");
        }

        return turnNo;
    }

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
