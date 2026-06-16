namespace server.domain.quest.enums;

/// <summary>
/// ステージの進行形態。Static=フロアを事前定義する有限ステージ、
/// Endless=フロア番号から動的生成する終わりなきステージ。
/// </summary>
public enum ProgressionType
{
    Static = 0,
    Endless = 1
}
