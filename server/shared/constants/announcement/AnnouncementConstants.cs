namespace server.shared.constants.announcement;

public static class AnnouncementConstants
{
    public const int IdMaxLength = 100;
    public const int TitleMaxLength = 100;
    public const int BodyMaxLength = 2000;

    /// <summary>お知らせの日付（表示用）を決めるときのタイムゾーン。プレイヤーは日本向け。</summary>
    public static readonly TimeSpan DisplayUtcOffset = TimeSpan.FromHours(9);
}
