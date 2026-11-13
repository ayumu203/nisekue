using System.Text.RegularExpressions;
using server.shared.constants.announcement;

namespace server.domain.announcement;

/// <summary>
/// お知らせの ID。削除時に指定するため、人が読み書きしやすい英小文字・数字・ハイフンのみを許可する。
/// </summary>
public sealed partial class AnnouncementId
{
    public AnnouncementId(string value)
    {
        Value = Validate(value);
    }

    public string Value { get; }

    /// <summary>日付と乱数から ID を採番する（例: 2026-11-13-a1b2c3d4）。</summary>
    public static AnnouncementId Generate(DateOnly date)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new AnnouncementId($"{date:yyyy-MM-dd}-{suffix}");
    }

    public override string ToString() => Value;

    private static string Validate(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length is < 1 or > AnnouncementConstants.IdMaxLength)
        {
            throw new DomainException($"お知らせIDは1文字から{AnnouncementConstants.IdMaxLength}文字以内です。");
        }

        if (!IdPattern().IsMatch(normalized))
        {
            throw new DomainException("お知らせIDは英小文字・数字・ハイフンのみ使用できます。");
        }

        return normalized;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex IdPattern();
}
