using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;
using server.infrastructure;
using server.shared.constants.player;

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
            ownedQuantity = quantityByItemId.GetValueOrDefault(ResolveTreasureMapItemId(map), 0),
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
        IEquipmentRepository equipmentRepository,
        IDbContextFactory<AppDbContext> dbContextFactory)
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
            equipmentRepository,
            dbContextFactory);

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

        var mapItemId = ResolveTreasureMapItemId(map);
        var stacks = await playerItemStackRepository.GetByPlayerAsync(playerId.Value);
        var stack = stacks.FirstOrDefault(x => x.ItemId == new ItemId(mapItemId));
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
        ITreasureMapRepository treasureMapRepository,
        ITreasureMapRewardPoolRepository rewardPoolRepository,
        IItemRepository itemRepository,
        IEquipmentRepository equipmentRepository,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user);
        if (playerId is null)
        {
            return Results.Unauthorized();
        }

        await using var lockDbContext = await dbContextFactory.CreateDbContextAsync();
        await using var lockTransaction = await lockDbContext.Database.BeginTransactionAsync();
        await lockDbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            BuildExpeditionLockKey(expeditionId));

        var expedition = await lockDbContext.TreasureMapExpeditions
            .FromSqlInterpolated($"SELECT * FROM internal.treasure_map_expeditions WHERE id = {expeditionId} FOR UPDATE")
            .SingleOrDefaultAsync();
        if (expedition is null)
        {
            return Results.NotFound(new { message = "遠征が見つかりません。" });
        }

        if (expedition.PlayerId != playerId.Value.Value)
        {
            return Results.Forbid();
        }

        if ((TreasureMapExpeditionStatus)expedition.Status == TreasureMapExpeditionStatus.InProgress &&
            expedition.EndsAt <= DateTimeOffset.UtcNow)
        {
            var map = await treasureMapRepository.GetAsync(new TreasureMapId(expedition.MapId))
                ?? throw new InvalidOperationException("宝の地図マスタが見つかりません。");
            var rewardPool = await rewardPoolRepository.GetAsync(map.RewardPoolId)
                ?? throw new InvalidOperationException("報酬プールが見つかりません。");

            var validItemIds = (await itemRepository.GetAllAsync()).Select(x => x.Id.Value).ToHashSet();
            var validEquipmentIds = (await equipmentRepository.GetAllAsync()).Select(x => x.Id.Value).ToHashSet();
            var random = new Random(BuildRewardSeed(expedition.Id, expedition.PlayerId, expedition.MapId));
            var rewardResult = BuildRewardResult(rewardPool, validItemIds, validEquipmentIds, random);

            expedition.Status = (int)TreasureMapExpeditionStatus.Completed;
            expedition.CompletedAt = DateTimeOffset.UtcNow;
            expedition.RewardSummaryJson = SerializeRewardResult(rewardResult);
            expedition.UpdatedAt = DateTimeOffset.UtcNow;
        }

        if ((TreasureMapExpeditionStatus)expedition.Status != TreasureMapExpeditionStatus.Completed)
        {
            return Results.BadRequest(new { message = "遠征が完了していません。" });
        }

        if (expedition.RewardClaimed)
        {
            return Results.Conflict(new { message = "報酬は既に受け取り済みです。" });
        }

        if (await lockDbContext.TreasureMapClaimHistories.AnyAsync(x => x.ExpeditionId == expedition.Id))
        {
            return Results.Conflict(new { message = "この遠征の報酬は既に受け取り済みです。" });
        }

        var reward = DeserializeRewardResult(expedition.RewardSummaryJson);
        if (reward is null)
        {
            return Results.Conflict(new { message = "報酬情報が存在しません。" });
        }

        var player = await lockDbContext.Players
            .FromSqlInterpolated($"SELECT * FROM internal.players WHERE id = {playerId.Value.Value} FOR UPDATE")
            .SingleOrDefaultAsync();
        if (player is null)
        {
            return Results.NotFound(new { message = "プレイヤーが見つかりません。" });
        }

        var allItems = await itemRepository.GetAllAsync();
        var itemById = allItems.ToDictionary(x => x.Id.Value, x => x);
        var allEquipment = await equipmentRepository.GetAllAsync();
        var equipmentById = allEquipment.ToDictionary(x => x.Id.Value, x => x);

        var now = DateTimeOffset.UtcNow;

        // ここは Player 集約を経由せず PlayerEntity を直接更新するため、レベル上限の判定を明示的に行う。
        if (reward.ExperiencePoints > 0 && player.Level < PlayerConstants.MaxLevel)
        {
            player.Exp = (int)Math.Min((long)player.Exp + reward.ExperiencePoints, int.MaxValue);
        }

        if (reward.Gold > 0)
        {
            player.Gold = (int)Math.Min((long)player.Gold + reward.Gold, int.MaxValue);
        }

        var stacks = await lockDbContext.PlayerItemStacks
            .Where(x => x.PlayerId == player.Id)
            .ToListAsync();

        var itemQuantitySummary = new Dictionary<int, int>();
        if (reward.ItemIds.Count > 0)
        {
            foreach (var group in reward.ItemIds.GroupBy(x => x))
            {
                if (!itemById.TryGetValue(group.Key, out var itemMaster))
                {
                    continue;
                }

                var quantity = group.Count();
                itemQuantitySummary[group.Key] = quantity;

                var existing = stacks.FirstOrDefault(x => x.ItemId == group.Key);
                if (existing is null)
                {
                    var initialQuantity = Math.Min(quantity, itemMaster.MaxStack);
                    var created = new server.infrastructure.player.PlayerItemStackEntity
                    {
                        Id = Guid.NewGuid(),
                        PlayerId = player.Id,
                        ItemId = group.Key,
                        Quantity = initialQuantity,
                        UpdatedAt = now
                    };
                    stacks.Add(created);
                    lockDbContext.PlayerItemStacks.Add(created);
                }
                else
                {
                    existing.Quantity = Math.Min(existing.Quantity + quantity, itemMaster.MaxStack);
                    existing.UpdatedAt = now;
                }
            }
        }

        var equipmentSummary = new List<int>();
        if (reward.EquipmentIds.Count > 0)
        {
            foreach (var equipmentId in reward.EquipmentIds)
            {
                if (!equipmentById.TryGetValue(equipmentId, out var equipmentMaster))
                {
                    continue;
                }

                equipmentSummary.Add(equipmentId);
                lockDbContext.PlayerEquipments.Add(new server.infrastructure.player.PlayerEquipmentEntity
                {
                    Id = Guid.NewGuid(),
                    PlayerId = player.Id,
                    EquipmentId = equipmentMaster.Id.Value,
                    EquipmentType = (int)equipmentMaster.Type,
                    EquipmentStatus = (int)EquipmentStatus.Inventory,
                    Durability = equipmentMaster.MaxDurability,
                    Mastery = 0,
                    PlusValue = 0,
                    AcquiredAt = now,
                    UpdatedAt = now
                });
            }
        }

        expedition.RewardClaimed = true;
        expedition.Status = (int)TreasureMapExpeditionStatus.Claimed;
        expedition.UpdatedAt = now;

        var rewardSummaryJson = JsonSerializer.Serialize(new
        {
            itemQuantities = itemQuantitySummary,
            equipmentIds = equipmentSummary,
            experiencePoints = reward.ExperiencePoints,
            gold = reward.Gold
        });

        lockDbContext.TreasureMapClaimHistories.Add(new server.infrastructure.treasuremap.TreasureMapClaimHistoryEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ExpeditionId = expedition.Id,
            RewardSummaryJson = rewardSummaryJson,
            ClaimedAt = now
        });

        await lockDbContext.SaveChangesAsync();

        await lockTransaction.CommitAsync();

        return Results.Ok(new
        {
            message = "宝の地図報酬を受け取りました。",
            expedition = new
            {
                expeditionId = expedition.Id,
                mapId = expedition.MapId,
                startedAt = expedition.StartedAt,
                endsAt = expedition.EndsAt,
                status = TreasureMapExpeditionStatus.Claimed.ToString(),
                rewardClaimed = expedition.RewardClaimed,
                completedAt = expedition.CompletedAt,
                reward = new
                {
                    itemIds = reward.ItemIds,
                    equipmentIds = reward.EquipmentIds,
                    experiencePoints = reward.ExperiencePoints,
                    gold = reward.Gold
                }
            }
        });
    }

    private static async Task<TreasureMapExpedition> EnsureExpeditionCompletedAsync(
        TreasureMapExpedition expedition,
        ITreasureMapRepository treasureMapRepository,
        ITreasureMapRewardPoolRepository rewardPoolRepository,
        ITreasureMapExpeditionRepository expeditionRepository,
        IItemRepository itemRepository,
        IEquipmentRepository equipmentRepository,
        IDbContextFactory<AppDbContext> dbContextFactory,
        bool alreadyLocked = false)
    {
        if (expedition.Status != TreasureMapExpeditionStatus.InProgress || expedition.EndsAt > DateTimeOffset.UtcNow)
        {
            return expedition;
        }

        if (!alreadyLocked)
        {
            await using var lockDbContext = await dbContextFactory.CreateDbContextAsync();
            await using var lockTransaction = await lockDbContext.Database.BeginTransactionAsync();
            await lockDbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0})",
                BuildExpeditionLockKey(expedition.Id.Value));

            var lockedExpedition = await expeditionRepository.GetAsync(expedition.Id)
                ?? throw new InvalidOperationException("遠征データが見つかりません。");
            var completed = await EnsureExpeditionCompletedAsync(
                lockedExpedition,
                treasureMapRepository,
                rewardPoolRepository,
                expeditionRepository,
                itemRepository,
                equipmentRepository,
                dbContextFactory,
                alreadyLocked: true);

            await lockTransaction.CommitAsync();
            return completed;
        }

        var map = await treasureMapRepository.GetAsync(expedition.MapId)
            ?? throw new InvalidOperationException("宝の地図マスタが見つかりません。");
        var rewardPool = await rewardPoolRepository.GetAsync(map.RewardPoolId)
            ?? throw new InvalidOperationException("報酬プールが見つかりません。");

        var itemIds = (await itemRepository.GetAllAsync()).Select(x => x.Id.Value).ToHashSet();
        var equipmentIds = (await equipmentRepository.GetAllAsync()).Select(x => x.Id.Value).ToHashSet();

        var random = new Random(BuildRewardSeed(expedition.Id.Value, expedition.PlayerId.Value, expedition.MapId.Value));
        var rewardResult = BuildRewardResult(rewardPool, itemIds, equipmentIds, random);

        expedition.MarkCompleted(rewardResult, DateTimeOffset.UtcNow);
        await expeditionRepository.SaveAsync(expedition);

        return expedition;
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

    private static TreasureMapRewardResult BuildRewardResult(
        TreasureMapRewardPool rewardPool,
        IReadOnlySet<int> validItemIds,
        IReadOnlySet<int> validEquipmentIds,
        Random random)
    {
        // Item/Equipment is probabilistic, Gold/Experience are guaranteed.
        var lootEntry = ResolveRewardEntryByTypes(
            rewardPool,
            validItemIds,
            validEquipmentIds,
            [TreasureMapRewardType.Item, TreasureMapRewardType.Equipment],
            random);
        var goldEntry = ResolveRewardEntryByTypes(
            rewardPool,
            validItemIds,
            validEquipmentIds,
            [TreasureMapRewardType.Gold],
            random);
        var experienceEntry = ResolveRewardEntryByTypes(
            rewardPool,
            validItemIds,
            validEquipmentIds,
            [TreasureMapRewardType.Experience],
            random);

        var itemIds = Array.Empty<int>();
        var equipmentIds = Array.Empty<int>();

        if (lootEntry is not null)
        {
            if (lootEntry.RewardType == TreasureMapRewardType.Item)
            {
                var itemId = lootEntry.ItemId ?? throw new InvalidOperationException("itemId が未設定です。");
                var min = lootEntry.QuantityMin ?? 1;
                var max = lootEntry.QuantityMax ?? min;
                itemIds = Enumerable.Repeat(itemId, random.Next(min, max + 1)).ToArray();
            }
            else if (lootEntry.RewardType == TreasureMapRewardType.Equipment)
            {
                var equipmentId = lootEntry.EquipmentId ?? throw new InvalidOperationException("equipmentId が未設定です。");
                equipmentIds = [equipmentId];
            }
        }

        return new TreasureMapRewardResult(
            itemIds: itemIds,
            equipmentIds: equipmentIds,
            experiencePoints: experienceEntry?.ExperienceAmount ?? 0,
            gold: goldEntry?.GoldAmount ?? 0);
    }

    private static TreasureMapRewardEntry? ResolveRewardEntryByTypes(
        TreasureMapRewardPool rewardPool,
        IReadOnlySet<int> validItemIds,
        IReadOnlySet<int> validEquipmentIds,
        IReadOnlyCollection<TreasureMapRewardType> targetTypes,
        Random random)
    {
        var candidates = rewardPool.Entries
            .Where(entry => targetTypes.Contains(entry.RewardType) && IsValidEntry(entry, validItemIds, validEquipmentIds))
            .ToArray();

        if (candidates.Length == 0)
        {
            return null;
        }

        var totalWeight = candidates.Sum(x => x.Weight);
        if (totalWeight <= 0)
        {
            return candidates[0];
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

        return candidates[^1];
    }

    private static string SerializeRewardResult(TreasureMapRewardResult reward)
    {
        return JsonSerializer.Serialize(new
        {
            itemIds = reward.ItemIds,
            equipmentIds = reward.EquipmentIds,
            experiencePoints = reward.ExperiencePoints,
            gold = reward.Gold
        });
    }

    private static TreasureMapRewardResult? DeserializeRewardResult(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var itemIds = root.TryGetProperty("itemIds", out var itemIdsElement)
            ? itemIdsElement.EnumerateArray().Select(x => x.GetInt32()).ToArray()
            : Array.Empty<int>();
        var equipmentIds = root.TryGetProperty("equipmentIds", out var equipmentIdsElement)
            ? equipmentIdsElement.EnumerateArray().Select(x => x.GetInt32()).ToArray()
            : Array.Empty<int>();
        var exp = root.TryGetProperty("experiencePoints", out var expElement) ? expElement.GetInt32() : 0;
        var gold = root.TryGetProperty("gold", out var goldElement) ? goldElement.GetInt32() : 0;

        return new TreasureMapRewardResult(itemIds, equipmentIds, exp, gold);
    }

    private static int ResolveTreasureMapItemId(TreasureMap map)
    {
        if (int.TryParse(map.Code, out var parsedCode))
        {
            return parsedCode;
        }

        return map.Id.Value;
    }

    private static long BuildExpeditionLockKey(Guid expeditionId)
    {
        return BitConverter.ToInt64(expeditionId.ToByteArray(), 0);
    }

    private static int BuildRewardSeed(Guid expeditionId, Guid playerId, int mapId)
    {
        return HashCode.Combine(expeditionId, playerId, mapId);
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

        var lootEntries = pool.Entries
            .Where(x => x.RewardType is TreasureMapRewardType.Item or TreasureMapRewardType.Equipment)
            .ToArray();

        var totalWeight = lootEntries.Sum(x => x.Weight);
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
            itemRate = RateByType(lootEntries, TreasureMapRewardType.Item, totalWeight),
            equipmentRate = RateByType(lootEntries, TreasureMapRewardType.Equipment, totalWeight),
            experienceRate = 0d,
            goldRate = 0d
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
