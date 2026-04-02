using System;
using System.Collections.Generic;

namespace server.domain.treasuremap;

public sealed class TreasureMapRewardResult
{
    private readonly IReadOnlyList<int> itemIds;
    private readonly IReadOnlyList<int> equipmentIds;

    public TreasureMapRewardResult(
        IEnumerable<int>? itemIds = null,
        IEnumerable<int>? equipmentIds = null,
        int experiencePoints = 0,
        int gold = 0)
    {
        this.itemIds = (itemIds ?? Array.Empty<int>()).ToArray();
        this.equipmentIds = (equipmentIds ?? Array.Empty<int>()).ToArray();
        ExperiencePoints = ValidateNonNegative(experiencePoints);
        Gold = ValidateNonNegative(gold);
    }

    public IReadOnlyList<int> ItemIds => itemIds;
    public IReadOnlyList<int> EquipmentIds => equipmentIds;
    public int ExperiencePoints { get; }
    public int Gold { get; }

    public bool HasRewards => ItemIds.Count > 0 || EquipmentIds.Count > 0 || ExperiencePoints > 0 || Gold > 0;

    private static int ValidateNonNegative(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Reward value must be non-negative.");
        }

        return value;
    }
}
