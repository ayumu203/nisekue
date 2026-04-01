using System.Security.Claims;
using System.Text.Json;
using server.domain.player;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;

namespace server.endpoints;

internal static class TreasureMapEndpoints
{
    internal static WebApplication MapTreasureMapEndpoints(this WebApplication app)
    {
        app.MapGet("/treasure-maps", GetTreasureMaps).RequireAuthorization();
        app.MapGet("/treasure-map-expeditions/current", GetCurrentExpedition).RequireAuthorization();
        app.MapPost("/treasure-map-expeditions", StartExpedition).RequireAuthorization();
        app.MapPost("/treasure-map-expeditions/{expeditionId:guid}/claim", ClaimReward).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetTreasureMaps(
        ClaimsPrincipal user,
        ITreasureMapRepository treasureMapRepository,
        IPlayerItemStackRepository playerItemStackRepository,
        ITreasureMapRewardPoolRepository rewardPoolRepository,
        IItemRepository itemRepository,
        IEquipmentRepository equipmentRepository)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user);
        if (playerId is null)
        {
            return Results.Unauthorized();
        }

        var maps = await treasureMapRepository.GetAllAsync();
        var stacks = await playerItemStackRepository.GetByPlayerAsync(playerId.Value);
        var quantityByItemId = stacks.ToDictionary(x => x.ItemId.Value, x => x.Quantity);
        var rewardPools = await rewardPoolRepository.GetAllAsync();
        var rewardPoolById = rewardPools.ToDictionary(x => x.Id, x => x);
        var itemNameById = (await itemRepository.GetAllAsync()).ToDictionary(x => x.Id.Value, x => x.Name);
        var equipmentNameById = (await equipmentRepository.GetAllAsync()).ToDictionary(x => x.Id.Value, x => x.Name);

        return Results.Ok(maps.Select(map => new
        {
            mapId = map.Id.Value,
            code = map.Code,
            name = map.Name,
            description = map.Description,
            narrativeText = map.Description,
            grade = map.Grade.ToString(),
            durationSeconds = map.DurationSeconds,
            isMarketable = map.IsMarketable,
            isHiddenFromInventory = map.IsHiddenFromInventory,
            ownedQuantity = quantityByItemId.GetValueOrDefault(map.Id.Value, 0),
            rewardTendency = BuildRewardTendency(map.RewardPoolId, rewardPoolById),
            rewardCandidates = BuildRewardCandidates(map.RewardPoolId, rewardPoolById, itemNameById, equipmentNameById)
        }));
    }

    private static async Task<IResult> GetCurrentExpedition(
        ClaimsPrincipal user,
        IPlayerRepository playerRepository,
        ITreasureMapRepository treasureMapRepository,
        ITreasureMapRewardPoolRepository rewardPoolRepository,
        ITreasureMapExpeditionRepository expeditionRepository,
        IItemRepository itemRepository,
        IEquipmentRepository equipmentRepository)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user);
        if (playerId is null)
        {
            return Results.Unauthorized();
        }

        var player = await playerRepository.GetPlayerAsync(playerId.Value);
        if (player is null)
        {
            return Results.NotFound(new { message = "プレイヤーが見つかりません。" });
        }

        var expedition = await expeditionRepository.GetCurrentByPlayerAsync(playerId.Value);
        if (expedition is null)
        {
            return Results.Ok((object?)null);
        }

        expedition = await EnsureExpeditionCompletedAsync(
            expedition,
            treasureMapRepository,
            rewardPoolRepository,
            expeditionRepository,
            itemRepository,
            equipmentRepository);

        return Results.Ok(ToExpeditionResponse(expedition));
    }

    private static async Task<IResult> StartExpedition(
        ClaimsPrincipal user,
        StartTreasureMapExpeditionRequest request,
        IPlayerRepository playerRepository,
        ITreasureMapRepository treasureMapRepository,
        ITreasureMapExpeditionRepository expeditionRepository,
        IPlayerItemStackRepository playerItemStackRepository)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user);
        if (playerId is null)
        {
            return Results.Unauthorized();
        }

        var player = await playerRepository.GetPlayerAsync(playerId.Value);
        if (player is null)
        {
            return Results.NotFound(new { message = "プレイヤーが見つかりません。" });
        }

        var existing = await expeditionRepository.GetCurrentByPlayerAsync(playerId.Value);
        if (existing is not null)
        {
            if (existing.Status == TreasureMapExpeditionStatus.InProgress)
            {
                return Results.Conflict(new { message = "進行中の宝の地図遠征があります。" });
            }

            if (existing.Status == TreasureMapExpeditionStatus.Completed && !existing.RewardClaimed)
            {
                return Results.Conflict(new { message = "未受取の報酬があります。先に受け取ってください。" });
            }
        }

        var map = await treasureMapRepository.GetAsync(new TreasureMapId(request.MapId));
        if (map is null)
        {
            return Results.NotFound(new { message = "宝の地図が見つかりません。" });
        }

        var stacks = await playerItemStackRepository.GetByPlayerAsync(playerId.Value);
        var stack = stacks.FirstOrDefault(x => x.ItemId == new ItemId(request.MapId));
        if (stack is null || stack.Quantity < 1)
        {
            return Results.BadRequest(new { message = "対象の宝の地図を所持していません。" });
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            stack.ConsumeQuantity(1, now);
            if (stack.Quantity == 0)
            {
                await playerItemStackRepository.DeleteAsync(stack.Id);
            }
            else
            {
                await playerItemStackRepository.SaveAsync([stack]);
            }

            var expedition = map.StartExpedition(playerId.Value, now);
            await expeditionRepository.SaveAsync(expedition);

            return Results.Ok(ToExpeditionResponse(expedition));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ClaimReward(
        ClaimsPrincipal user,
        Guid expeditionId,
        IPlayerRepository playerRepository,
        ITreasureMapRepository treasureMapRepository,
        ITreasureMapRewardPoolRepository rewardPoolRepository,
        ITreasureMapExpeditionRepository expeditionRepository,
        ITreasureMapClaimHistoryRepository claimHistoryRepository,
        IPlayerItemStackRepository playerItemStackRepository,
        IPlayerEquipmentRepository playerEquipmentRepository,
        IItemRepository itemRepository,
        IEquipmentRepository equipmentRepository)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user);
        if (playerId is null)
        {
            return Results.Unauthorized();
        }

        var player = await playerRepository.GetPlayerAsync(playerId.Value);
        if (player is null)
        {
            return Results.NotFound(new { message = "プレイヤーが見つかりません。" });
        }

        var expedition = await expeditionRepository.GetAsync(new TreasureMapExpeditionId(expeditionId));
        if (expedition is null)
        {
            return Results.NotFound(new { message = "遠征が見つかりません。" });
        }

        if (expedition.PlayerId != playerId.Value)
        {
            return Results.Forbid();
        }

        expedition = await EnsureExpeditionCompletedAsync(
            expedition,
            treasureMapRepository,
            rewardPoolRepository,
            expeditionRepository,
            itemRepository,
            equipmentRepository);

        if (expedition.Status != TreasureMapExpeditionStatus.Completed)
        {
            return Results.BadRequest(new { message = "遠征が完了していません。" });
        }

        if (expedition.RewardClaimed)
        {
            return Results.Conflict(new { message = "報酬は既に受け取り済みです。" });
        }

        if (await claimHistoryRepository.ExistsByExpeditionAsync(expedition.Id))
        {
            return Results.Conflict(new { message = "この遠征の報酬は既に受け取り済みです。" });
        }

        var reward = expedition.RewardResult;
        if (reward is null)
        {
            return Results.Conflict(new { message = "報酬情報が存在しません。" });
        }

        var allItems = await itemRepository.GetAllAsync();
        var itemById = allItems.ToDictionary(x => x.Id.Value, x => x);
        var allEquipment = await equipmentRepository.GetAllAsync();
        var equipmentById = allEquipment.ToDictionary(x => x.Id.Value, x => x);

        var now = DateTimeOffset.UtcNow;

        if (reward.ExperiencePoints > 0)
        {
            player.GainExp(reward.ExperiencePoints);
        }

        if (reward.Gold > 0)
        {
            player.GainGold(reward.Gold);
        }

        await playerRepository.SaveAsync(player);

        var itemsToSave = new List<PlayerItemStack>();
        var itemQuantitySummary = new Dictionary<int, int>();
        if (reward.ItemIds.Count > 0)
        {
            var stacks = (await playerItemStackRepository.GetByPlayerAsync(player.Id)).ToList();
            foreach (var group in reward.ItemIds.GroupBy(x => x))
            {
                if (!itemById.TryGetValue(group.Key, out var itemMaster))
                {
                    continue;
                }

                var quantity = group.Count();
                itemQuantitySummary[group.Key] = quantity;

                var existing = stacks.FirstOrDefault(x => x.ItemId.Value == group.Key);
                if (existing is null)
                {
                    var initialQuantity = Math.Min(quantity, itemMaster.MaxStack);
                    var created = new PlayerItemStack(
                        PlayerItemStackId.New(),
                        player.Id,
                        new ItemId(group.Key),
                        initialQuantity,
                        now);
                    stacks.Add(created);
                    itemsToSave.Add(created);
                }
                else
                {
                    existing.AddQuantity(quantity, itemMaster.MaxStack, now);
                    itemsToSave.Add(existing);
                }
            }

            if (itemsToSave.Count > 0)
            {
                await playerItemStackRepository.SaveAsync(itemsToSave);
            }
        }

        var equipmentSummary = new List<int>();
        if (reward.EquipmentIds.Count > 0)
        {
            var equipmentsToSave = new List<PlayerEquipment>();
            foreach (var equipmentId in reward.EquipmentIds)
            {
                if (!equipmentById.TryGetValue(equipmentId, out var equipmentMaster))
                {
                    continue;
                }

                equipmentSummary.Add(equipmentId);
                equipmentsToSave.Add(new PlayerEquipment(
                    PlayerEquipmentId.New(),
                    player.Id,
                    equipmentMaster.Id,
                    equipmentMaster.Type,
                    EquipmentStatus.Inventory,
                    equipmentMaster.MaxDurability,
                    0,
                    now,
                    now));
            }

            if (equipmentsToSave.Count > 0)
            {
                await playerEquipmentRepository.SaveAsync(equipmentsToSave);
            }
        }

        expedition.ClaimReward();
        await expeditionRepository.SaveAsync(expedition);

        var rewardSummaryJson = JsonSerializer.SerializeToDocument(new
        {
            itemQuantities = itemQuantitySummary,
            equipmentIds = equipmentSummary,
            experiencePoints = reward.ExperiencePoints,
            gold = reward.Gold
        });

        await claimHistoryRepository.AddAsync(new TreasureMapClaimHistory(
            Guid.NewGuid(),
            player.Id,
            expedition.Id,
            rewardSummaryJson,
            now));

        return Results.Ok(new
        {
            message = "宝の地図報酬を受け取りました。",
            expedition = ToExpeditionResponse(expedition)
        });
    }

    private static async Task<TreasureMapExpedition> EnsureExpeditionCompletedAsync(
        TreasureMapExpedition expedition,
        ITreasureMapRepository treasureMapRepository,
        ITreasureMapRewardPoolRepository rewardPoolRepository,
        ITreasureMapExpeditionRepository expeditionRepository,
        IItemRepository itemRepository,
        IEquipmentRepository equipmentRepository)
    {
        if (expedition.Status != TreasureMapExpeditionStatus.InProgress || expedition.EndsAt > DateTimeOffset.UtcNow)
        {
            return expedition;
        }

        var map = await treasureMapRepository.GetAsync(expedition.MapId)
            ?? throw new InvalidOperationException("宝の地図マスタが見つかりません。");
        var rewardPool = await rewardPoolRepository.GetAsync(map.RewardPoolId)
            ?? throw new InvalidOperationException("報酬プールが見つかりません。");

        var itemIds = (await itemRepository.GetAllAsync()).Select(x => x.Id.Value).ToHashSet();
        var equipmentIds = (await equipmentRepository.GetAllAsync()).Select(x => x.Id.Value).ToHashSet();

        var drawnEntry = ResolveRewardEntry(rewardPool, itemIds, equipmentIds);
        var rewardResult = ToRewardResult(drawnEntry);

        expedition.MarkCompleted(rewardResult, DateTimeOffset.UtcNow);
        await expeditionRepository.SaveAsync(expedition);

        return expedition;
    }

    private static TreasureMapRewardEntry ResolveRewardEntry(
        TreasureMapRewardPool rewardPool,
        IReadOnlySet<int> validItemIds,
        IReadOnlySet<int> validEquipmentIds)
    {
        var random = Random.Shared;
        var drawn = rewardPool.Draw(random);

        if (IsValidEntry(drawn, validItemIds, validEquipmentIds))
        {
            return drawn;
        }

        var fallback = rewardPool.Entries.FirstOrDefault(entry =>
            entry.IsFallback
            && entry.RewardType == drawn.RewardType
            && IsValidEntry(entry, validItemIds, validEquipmentIds));

        if (fallback is not null)
        {
            return fallback;
        }

        throw new InvalidOperationException("報酬エントリの整合性が取れません。fallback を確認してください。");
    }

    private static bool IsValidEntry(
        TreasureMapRewardEntry entry,
        IReadOnlySet<int> validItemIds,
        IReadOnlySet<int> validEquipmentIds)
    {
        return entry.RewardType switch
        {
            TreasureMapRewardType.Item => entry.ItemId is not null && validItemIds.Contains(entry.ItemId.Value),
            TreasureMapRewardType.Equipment => entry.EquipmentId is not null && validEquipmentIds.Contains(entry.EquipmentId.Value),
            TreasureMapRewardType.Experience => entry.ExperienceAmount is > 0,
            TreasureMapRewardType.Gold => entry.GoldAmount is > 0,
            _ => false
        };
    }

    private static TreasureMapRewardResult ToRewardResult(TreasureMapRewardEntry entry)
    {
        return entry.RewardType switch
        {
            TreasureMapRewardType.Item => new TreasureMapRewardResult(
                itemIds: Enumerable.Repeat(
                    entry.ItemId ?? throw new InvalidOperationException("itemId が未設定です。"),
                    Random.Shared.Next(entry.QuantityMin ?? 1, (entry.QuantityMax ?? entry.QuantityMin ?? 1) + 1))),
            TreasureMapRewardType.Equipment => new TreasureMapRewardResult(
                equipmentIds: [entry.EquipmentId ?? throw new InvalidOperationException("equipmentId が未設定です。")]),
            TreasureMapRewardType.Experience => new TreasureMapRewardResult(
                experiencePoints: entry.ExperienceAmount ?? 0),
            TreasureMapRewardType.Gold => new TreasureMapRewardResult(
                gold: entry.GoldAmount ?? 0),
            _ => new TreasureMapRewardResult()
        };
    }

    private static object ToExpeditionResponse(TreasureMapExpedition expedition)
    {
        return new
        {
            expeditionId = expedition.Id.Value,
            mapId = expedition.MapId.Value,
            startedAt = expedition.StartedAt,
            endsAt = expedition.EndsAt,
            status = expedition.Status.ToString(),
            rewardClaimed = expedition.RewardClaimed,
            completedAt = expedition.CompletedAt,
            reward = expedition.RewardResult is null
                ? null
                : new
                {
                    itemIds = expedition.RewardResult.ItemIds,
                    equipmentIds = expedition.RewardResult.EquipmentIds,
                    experiencePoints = expedition.RewardResult.ExperiencePoints,
                    gold = expedition.RewardResult.Gold
                }
        };
    }

    private static object BuildRewardTendency(
        TreasureMapRewardPoolId rewardPoolId,
        IReadOnlyDictionary<TreasureMapRewardPoolId, TreasureMapRewardPool> rewardPoolById)
    {
        if (!rewardPoolById.TryGetValue(rewardPoolId, out var pool))
        {
            return new
            {
                itemRate = 0d,
                equipmentRate = 0d,
                experienceRate = 0d,
                goldRate = 0d
            };
        }

        var totalWeight = pool.Entries.Sum(x => x.Weight);
        if (totalWeight <= 0)
        {
            return new
            {
                itemRate = 0d,
                equipmentRate = 0d,
                experienceRate = 0d,
                goldRate = 0d
            };
        }

        static double RateByType(IReadOnlyList<TreasureMapRewardEntry> entries, TreasureMapRewardType type, int total)
        {
            var weight = entries.Where(x => x.RewardType == type).Sum(x => x.Weight);
            return Math.Round(weight * 100d / total, 1, MidpointRounding.AwayFromZero);
        }

        return new
        {
            itemRate = RateByType(pool.Entries, TreasureMapRewardType.Item, totalWeight),
            equipmentRate = RateByType(pool.Entries, TreasureMapRewardType.Equipment, totalWeight),
            experienceRate = RateByType(pool.Entries, TreasureMapRewardType.Experience, totalWeight),
            goldRate = RateByType(pool.Entries, TreasureMapRewardType.Gold, totalWeight)
        };
    }

    private static object BuildRewardCandidates(
        TreasureMapRewardPoolId rewardPoolId,
        IReadOnlyDictionary<TreasureMapRewardPoolId, TreasureMapRewardPool> rewardPoolById,
        IReadOnlyDictionary<int, string> itemNameById,
        IReadOnlyDictionary<int, string> equipmentNameById)
    {
        if (!rewardPoolById.TryGetValue(rewardPoolId, out var pool))
        {
            return new
            {
                items = Array.Empty<object>(),
                equipments = Array.Empty<object>(),
                experiences = Array.Empty<object>(),
                golds = Array.Empty<object>()
            };
        }

        var items = pool.Entries
            .Where(x => x.RewardType == TreasureMapRewardType.Item && x.ItemId is not null)
            .Select(x => new
            {
                itemId = x.ItemId!.Value,
                name = itemNameById.GetValueOrDefault(x.ItemId.Value, $"Item {x.ItemId.Value}"),
                quantityMin = x.QuantityMin ?? 1,
                quantityMax = x.QuantityMax ?? x.QuantityMin ?? 1,
                weight = x.Weight
            })
            .DistinctBy(x => x.itemId)
            .ToArray();

        var equipments = pool.Entries
            .Where(x => x.RewardType == TreasureMapRewardType.Equipment && x.EquipmentId is not null)
            .Select(x => new
            {
                equipmentId = x.EquipmentId!.Value,
                name = equipmentNameById.GetValueOrDefault(x.EquipmentId.Value, $"Equipment {x.EquipmentId.Value}"),
                weight = x.Weight
            })
            .DistinctBy(x => x.equipmentId)
            .ToArray();

        var experiences = pool.Entries
            .Where(x => x.RewardType == TreasureMapRewardType.Experience && x.ExperienceAmount is not null)
            .Select(x => new
            {
                amount = x.ExperienceAmount!.Value,
                weight = x.Weight
            })
            .DistinctBy(x => x.amount)
            .OrderBy(x => x.amount)
            .ToArray();

        var golds = pool.Entries
            .Where(x => x.RewardType == TreasureMapRewardType.Gold && x.GoldAmount is not null)
            .Select(x => new
            {
                amount = x.GoldAmount!.Value,
                weight = x.Weight
            })
            .DistinctBy(x => x.amount)
            .OrderBy(x => x.amount)
            .ToArray();

        return new
        {
            items,
            equipments,
            experiences,
            golds
        };
    }
}
