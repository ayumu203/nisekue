using server.shared.constants.announcement;

namespace server.domain.announcement;

/// <summary>
/// ホーム画面の「お知らせ」タブに表示するお知らせ。登録後の編集は行わない。
/// </summary>
public sealed class Announcement
{
    private Announcement(AnnouncementId id, DateOnly date, string title, string body, DateTimeOffset createdAt)
    {
        Id = id;
        Date = date;
        Title = title;
        Body = body;
        CreatedAt = createdAt;
    }

    public AnnouncementId Id { get; }
    public DateOnly Date { get; }
    public string Title { get; }
    public string Body { get; }
    public DateTimeOffset CreatedAt { get; }

    public static Announcement Create(AnnouncementId id, DateOnly date, string title, string body, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);

        var normalizedTitle = (title ?? string.Empty).Trim();
        if (normalizedTitle.Length is < 1 or > AnnouncementConstants.TitleMaxLength)
        {
            throw new DomainException($"タイトルは1文字から{AnnouncementConstants.TitleMaxLength}文字以内です。");
        }

        // 本文は改行コードを LF にそろえ、前後の空白・空行だけ取り除く
        var normalizedBody = (body ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (normalizedBody.Length is < 1 or > AnnouncementConstants.BodyMaxLength)
        {
            throw new DomainException($"本文は1文字から{AnnouncementConstants.BodyMaxLength}文字以内です。");
        }

        return new Announcement(id, date, normalizedTitle, normalizedBody, createdAt);
    }
}
