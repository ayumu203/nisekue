using System.Text.RegularExpressions;
using server.domain.player;
using server.shared.constants.chat;

namespace server.domain.chat;

/// <summary>
/// スレッド本文に付ける画像（1枚まで）。画像本体は R2 に置き、ここではキーだけを持つ。
/// </summary>
public sealed partial class ThreadImage
{
    public string Key { get; }
    public bool IsSensitive { get; private set; }

    public ThreadImage(string key, bool isSensitive)
    {
        Key = ValidateKey(key);
        IsSensitive = isSensitive;
    }

    /// <summary>保存先のキーを作る。キーはサーバーが決め、クライアントからは受け取らない。</summary>
    public static string CreateKey(PlayerId authorPlayerId, ThreadImageFormat format) =>
        $"threads/{authorPlayerId.Value:D}/{Guid.NewGuid():N}.{format.ToExtension()}";

    /// <summary>キーの形式が正しいか（ローカル保存のファイルパスにも使うため厳密に確認する）。</summary>
    public static bool IsValidKey(string? key) =>
        key is not null && key.Length <= ThreadConstants.ImageKeyMaxLength && KeyPattern().IsMatch(key);

    public void SetSensitive(bool isSensitive)
    {
        IsSensitive = isSensitive;
    }

    private static string ValidateKey(string key)
    {
        if (!IsValidKey(key))
        {
            throw new ArgumentException("画像キーの形式が正しくありません。", nameof(key));
        }

        return key;
    }

    [GeneratedRegex("^threads/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/[0-9a-f]{32}\\.(png|jpg)$")]
    private static partial Regex KeyPattern();
}
