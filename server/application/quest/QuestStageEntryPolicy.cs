using server.domain.player;
using server.domain.quest;

namespace server.application.quest;

internal static class QuestStageEntryPolicy
{
    private const decimal MinimumRecommendedLevelRatio = 0.7m;

    internal static bool MeetsMinimumLevel(Player player, QuestStageDefinition stage)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(stage);

        return player.Level >= GetMinimumAllowedLevel(stage);
    }

    internal static int GetMinimumAllowedLevel(QuestStageDefinition stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return stage.MinimumEntryLevel
            ?? Math.Max(1, (int)Math.Ceiling(stage.RecommendedLevel * MinimumRecommendedLevelRatio));
    }

    internal static bool MeetsMapUnlockRequirement(Player player, QuestStageDefinition stage)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(stage);

        return stage.RequiredMapUnlockFlag is null || player.HasMapUnlockFlag(stage.RequiredMapUnlockFlag.Value);
    }
}
