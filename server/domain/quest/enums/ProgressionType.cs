namespace server.domain.quest.enums;

/// <summary>
/// ステージの進行形態。Static=フロアを事前定義する有限ステージ、
/// Endless=フロア番号から動的生成する終わりなきステージ、
/// Mirror=プレイヤーを敵としてラン開始時に全フロア分を組み立てるステージ（名鑑の祠）。
/// </summary>
public enum ProgressionType
{
    Static = 0,
    Endless = 1,
    Mirror = 2
}
