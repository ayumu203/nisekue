using server.domain.announcement;
using server.shared.constants.announcement;

namespace server.application.announcement;

public sealed record PostAnnouncementCommand(string? Id, DateOnly? Date, string Title, string Body);

public sealed class AnnouncementService(IAnnouncementRepository announcementRepository)
{
    public Task<IReadOnlyList<Announcement>> ListAsync() => announcementRepository.ListAsync();

    /// <summary>
    /// お知らせを登録する。日付を省略した場合は JST の今日、ID を省略した場合は日付から自動採番する。
    /// </summary>
    /// <exception cref="AnnouncementIdConflictException">同じ ID のお知らせが既にある場合。</exception>
    public async Task<Announcement> PostAsync(PostAnnouncementCommand command, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(command);

        var date = command.Date ?? DateOnly.FromDateTime(now.ToOffset(AnnouncementConstants.DisplayUtcOffset).DateTime);
        var id = string.IsNullOrWhiteSpace(command.Id)
            ? AnnouncementId.Generate(date)
            : new AnnouncementId(command.Id);

        var announcement = Announcement.Create(id, date, command.Title, command.Body, now);
        if (!await announcementRepository.TryAddAsync(announcement))
        {
            throw new AnnouncementIdConflictException(id);
        }

        return announcement;
    }

    public Task<bool> DeleteAsync(string id) => announcementRepository.DeleteAsync(new AnnouncementId(id));
}

public sealed class AnnouncementIdConflictException(AnnouncementId id)
    : Exception($"お知らせID '{id.Value}' は既に使われています。")
{
    public AnnouncementId Id { get; } = id;
}
