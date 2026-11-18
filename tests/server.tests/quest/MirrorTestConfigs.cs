using server.domain.quest;

namespace server.tests.quest;

/// <summary>テスト用の名鑑の祠の調整値。値はマスタデータの初期値と同じ。</summary>
internal static class MirrorTestConfigs
{
    public static QuestMirrorConfig Default(
        int topPlayerCount = 20,
        int enemiesPerNormalFloor = 5,
        int bossCompanionCount = 3,
        int maxNormalAppearances = 2,
        int maxTotalAppearances = 3,
        double secondChoiceRate = 0.15,
        double selfBuffRate = 0.5,
        int selfHarmMinHpPercent = 20,
        int healHpPercent = 50,
        int restoreMpPercent = 30,
        int maxRevivesPerFloor = 3)
    {
        return new QuestMirrorConfig(
            topPlayerCount,
            enemiesPerNormalFloor,
            bossCompanionCount,
            maxNormalAppearances,
            maxTotalAppearances,
            secondChoiceRate,
            selfBuffRate,
            selfHarmMinHpPercent,
            healHpPercent,
            restoreMpPercent,
            maxRevivesPerFloor);
    }
}
