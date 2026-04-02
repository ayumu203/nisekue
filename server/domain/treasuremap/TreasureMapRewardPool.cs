using System;
using server.domain.treasuremap.enums;

namespace server.domain.treasuremap;

public sealed class TreasureMapRewardPool
{
    private readonly IReadOnlyList<TreasureMapRewardEntry> entries;

    public TreasureMapRewardPool(
        TreasureMapRewardPoolId id,
        string name,
        IReadOnlyList<TreasureMapRewardEntry> entries)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Reward pool name must not be empty.", nameof(name))
            : name.Trim();
        this.entries = entries ?? throw new ArgumentNullException(nameof(entries));

        if (this.entries.Count == 0)
        {
            throw new ArgumentException("Reward pool must have at least one entry.", nameof(entries));
        }
    }

    public TreasureMapRewardPoolId Id { get; }
    public string Name { get; }
    public IReadOnlyList<TreasureMapRewardEntry> Entries => entries;

    public TreasureMapRewardEntry Draw(Random random, Func<TreasureMapRewardEntry, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(random);

        var candidates = predicate is null
            ? entries
            : entries.Where(predicate).ToArray();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No reward entry matches the current draw condition.");
        }

        var totalWeight = candidates.Sum(x => x.Weight);
        if (totalWeight <= 0)
        {
            throw new InvalidOperationException("Reward entries must have a positive total weight.");
        }

        var roll = random.Next(0, totalWeight);
        var cumulative = 0;
        foreach (var entry in candidates)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
            {
                return entry;
            }
        }

        // Safe fallback for rounding/edge conditions.
        return candidates.Last();
    }

    public bool HasFallbackEntry(TreasureMapRewardType rewardType)
    {
        return entries.Any(x => x.RewardType == rewardType && x.IsFallback);
    }
}
