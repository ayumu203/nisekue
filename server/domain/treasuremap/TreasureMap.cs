using System;
using server.domain.player;
using server.domain.treasuremap.enums;

namespace server.domain.treasuremap;

public sealed class TreasureMap
{
    public TreasureMap(
        TreasureMapId id,
        string code,
        string name,
        string description,
        TreasureMapGrade grade,
        int durationSeconds,
        TreasureMapRewardPoolId rewardPoolId,
        bool isMarketable,
        bool isHiddenFromInventory,
        TreasureMapRewardPool? rewardPool = null)
    {
        Id = id;
        Code = ValidateCode(code);
        Name = ValidateName(name);
        Description = description?.Trim() ?? string.Empty;
        Grade = grade;
        DurationSeconds = ValidateDuration(durationSeconds);
        RewardPoolId = rewardPoolId;
        IsMarketable = isMarketable;
        IsHiddenFromInventory = isHiddenFromInventory;
        RewardPool = rewardPool;
    }

    public TreasureMapId Id { get; }
    public string Code { get; }
    public string Name { get; }
    public string Description { get; }
    public TreasureMapGrade Grade { get; }
    public int DurationSeconds { get; }
    public TreasureMapRewardPoolId RewardPoolId { get; }
    public bool IsMarketable { get; }
    public bool IsHiddenFromInventory { get; }
    public TreasureMapRewardPool? RewardPool { get; private set; }

    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

    public void AttachRewardPool(TreasureMapRewardPool rewardPool)
    {
        RewardPool = rewardPool ?? throw new ArgumentNullException(nameof(rewardPool));
        if (rewardPool.Id != RewardPoolId)
        {
            throw new InvalidOperationException("Reward pool identity does not match the reference.");
        }
    }

    public TreasureMapExpedition StartExpedition(PlayerId playerId, DateTimeOffset now)
    {
        ValidatePlayer(playerId);
        var expedition = new TreasureMapExpedition(
            TreasureMapExpeditionId.New(),
            Id,
            playerId,
            now,
            now.AddSeconds(DurationSeconds));

        return expedition;
    }

    private static string ValidateCode(string code)
    {
        var normalized = code?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Treasure map code must not be empty.", nameof(code));
        }

        return normalized;
    }

    private static string ValidateName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Treasure map name must not be empty.", nameof(name));
        }

        return normalized;
    }

    private static int ValidateDuration(int durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be positive.");
        }

        return durationSeconds;
    }

    private static void ValidatePlayer(PlayerId playerId)
    {
        if (playerId.Value == Guid.Empty)
        {
            throw new ArgumentException("PlayerId must be set.", nameof(playerId));
        }
    }
}
