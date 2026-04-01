using System;
using server.domain.treasuremap.enums;

namespace server.domain.treasuremap;

public sealed class TreasureMapRewardEntry
{
    public TreasureMapRewardEntry(
        TreasureMapRewardType rewardType,
        int weight,
        int? itemId = null,
        int? equipmentId = null,
        int? quantityMin = null,
        int? quantityMax = null,
        int? goldAmount = null,
        int? experienceAmount = null,
        bool isFallback = false)
    {
        RewardType = rewardType;
        Weight = ValidatePositive(weight, nameof(weight));
        ItemId = ValidateNullableNonNegative(itemId, nameof(itemId));
        EquipmentId = ValidateNullableNonNegative(equipmentId, nameof(equipmentId));
        QuantityMin = ValidateNullableNonNegative(quantityMin, nameof(quantityMin));
        QuantityMax = ValidateNullableNonNegative(quantityMax, nameof(quantityMax));
        GoldAmount = ValidateNullableNonNegative(goldAmount, nameof(goldAmount));
        ExperienceAmount = ValidateNullableNonNegative(experienceAmount, nameof(experienceAmount));
        IsFallback = isFallback;

        if (QuantityMin is not null && QuantityMax is not null && QuantityMin > QuantityMax)
        {
            throw new ArgumentException("QuantityMin must be less than or equal to QuantityMax.");
        }
    }

    public TreasureMapRewardType RewardType { get; }
    public int? ItemId { get; }
    public int? EquipmentId { get; }
    public int Weight { get; }
    public int? QuantityMin { get; }
    public int? QuantityMax { get; }
    public int? GoldAmount { get; }
    public int? ExperienceAmount { get; }
    public bool IsFallback { get; }

    private static int ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");
        }

        return value;
    }

    private static int? ValidateNullableNonNegative(int? value, string paramName)
    {
        if (value is not null && value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value;
    }
}
