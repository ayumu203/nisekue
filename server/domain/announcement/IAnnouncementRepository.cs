namespace server.domain.announcement;

public interface IAnnouncementRepository
{
    /// <summary>日付の新しい順（同じ日付は登録の新しい順）で全件返す。</summary>
    Task<IReadOnlyList<Announcement>> ListAsync();

    /// <summary>登録する。同じ ID が既にある場合は登録せず false を返す。</summary>
    Task<bool> TryAddAsync(Announcement announcement);

    /// <summary>削除する。該当がなければ false を返す。</summary>
    Task<bool> DeleteAsync(AnnouncementId id);
}
