using Microsoft.Extensions.Configuration;

namespace server.application.debug;

/// <summary>
/// デバッグ機能の設定（appsettings の Debug セクション）。
/// </summary>
/// <param name="Enabled">false ならデバッグ用の API を登録しない（本番の保険）。既定は true。</param>
/// <param name="AlwaysEnabled">true なら role を見ずに誰でも使える（ローカル用）。既定は false。</param>
public sealed record DebugSettings(bool Enabled, bool AlwaysEnabled)
{
    public const string SectionName = "Debug";

    public static DebugSettings FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return new DebugSettings(
            Enabled: section.GetValue("Enabled", defaultValue: true),
            AlwaysEnabled: section.GetValue("AlwaysEnabled", defaultValue: false));
    }
}
