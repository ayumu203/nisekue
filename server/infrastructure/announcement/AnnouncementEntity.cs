namespace server.infrastructure.announcement;

public sealed class AnnouncementEntity
{
    public string Id { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
