using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using server.application.chat;
using server.application.maintenance;
using server.application.player;
using server.domain.move;
using server.domain.player;
using server.domain.treasuremap;
using server.infrastructure;
using server.infrastructure.player;

namespace server.endpoints;

internal static class ItemEndpoints
{
    private const int ItemCapacity = 20;
    private const string MaintenanceTokenHeaderName = "X-Maintenance-Token";

    internal static WebApplication MapItemEndpoints(this WebApplication app)
    {
        app.MapGet("/items", async (
            ClaimsPrincipal user,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IEquipmentRepository equipmentRepository,
            IPlayerItemStackRepository playerItemStackRepository,
            IItemRepository itemRepository,
            ITreasureMapRepository treasureMapRepository,
            IMarketListingRepository marketListingRepository) =>
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

            var playerEquipments = await playerEquipmentRepository.GetByPlayerAsync(player.Id);
            var equipmentMasters = await equipmentRepository.GetAllAsync();
            var equipmentById = equipmentMasters.ToDictionary(x => x.Id);
            var itemStacks = await playerItemStackRepository.GetByPlayerAsync(player.Id);
            var items = await itemRepository.GetAllAsync();
            var itemById = items.ToDictionary(x => x.Id);
            var treasureMapItemIds = await ResolveTreasureMapItemIdsAsync(treasureMapRepository);
            var activeListings = await marketListingRepository.GetBySellerAsync(player.Id, DateTimeOffset.UtcNow);
            var listedEquipmentIds = activeListings
                .Where(x => x.PlayerEquipmentId is not null)
                .Select(x => x.PlayerEquipmentId!.Value)
                .ToHashSet();

            var equippedItems = playerEquipments
                .Where(x => x.Status == EquipmentStatus.Equipped)
                .Select(x => ToEquipmentView(x, equipmentById))
                .Where(x => x is not null)
                .ToArray();

            var inventoryEquipments = playerEquipments
                .Where(x => x.Status != EquipmentStatus.Equipped)
                .Where(x => !listedEquipmentIds.Contains(x.Id))
                .Select(x => ToEquipmentView(x, equipmentById))
                .Where(x => x is not null)
                .ToArray();

            var inventoryItems = itemStacks
                .Select(stack => ToItemStackView(stack, itemById, treasureMapItemIds))
                .Where(x => x is not null)
                .ToArray();

            return Results.Ok(new
            {
                capacity = ItemCapacity,
                usedSlots = CalculateUsedSlots(playerEquipments, itemStacks, listedEquipmentIds),
                gold = player.Gold,
                equippedItems,
                inventoryItems = inventoryEquipments.Concat(inventoryItems!).ToArray()
            });
        }).RequireAuthorization();

        app.MapPost("/items/{itemStackId:guid}/use", async (
            ClaimsPrincipal user,
            Guid itemStackId,
            UseItemRequest request,
            IPlayerRepository playerRepository,
            IPlayerItemStackRepository playerItemStackRepository,
            IItemRepository itemRepository,
            ITreasureMapRepository treasureMapRepository,
            IJobProfileRepository jobProfileRepository,
            IJobMoveLearningRuleRepository jobMoveLearningRuleRepository,
            ItemStatBoostService itemStatBoostService) =>
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

            var stack = await playerItemStackRepository.GetAsync(new PlayerItemStackId(itemStackId));
            if (stack is null || stack.PlayerId != player.Id)
            {
                return Results.NotFound(new { message = "アイテムスタックが見つかりません。" });
            }

            var item = await itemRepository.GetAsync(stack.ItemId);
            if (item is null)
            {
                return Results.BadRequest(new { message = "アイテムマスタが見つかりません。" });
            }

            if (await IsTreasureMapItemAsync(item.Id, treasureMapRepository))
            {
                return Results.BadRequest(new { message = "宝の地図は専用メニューから使用してください。" });
            }

            if (request.Quantity <= 0)
            {
                return Results.BadRequest(new { message = "使用数は1以上で指定してください。" });
            }

            if (!item.CanUse(player))
            {
                return Results.BadRequest(new { message = "このアイテムを使用する条件を満たしていません。" });
            }

            try
            {
                switch (item.EffectType)
                {
                    case ItemEffectType.StatBoost:
                        {
                            player.UpdateStatus(itemStatBoostService.Apply(player.Status, item, request.Quantity));
                            break;
                        }
                    case ItemEffectType.ChangeJob:
                        {
                            if (request.Quantity != 1)
                            {
                                return Results.BadRequest(new { message = "転職アイテムは1個ずつのみ使用できます。" });
                            }

                            if (item.ChangeJobTo is null)
                            {
                                return Results.BadRequest(new { message = "転職先ジョブが定義されていません。" });
                            }

                            var jobProfile = jobProfileRepository.GetByJob(item.ChangeJobTo.Value);
                            var learningRule = jobMoveLearningRuleRepository.GetByJob(item.ChangeJobTo.Value);
                            player.ChangeJob(item.ChangeJobTo.Value, jobProfile, learningRule, ignoreRequirements: true);
                            break;
                        }
                    default:
                        return Results.BadRequest(new { message = "未対応のアイテム効果です。" });
                }

                stack.ConsumeQuantity(request.Quantity, DateTimeOffset.UtcNow);
                await playerRepository.SaveAsync(player);
                if (stack.Quantity == 0)
                {
                    await playerItemStackRepository.DeleteAsync(stack.Id);
                }
                else
                {
                    await playerItemStackRepository.SaveAsync([stack]);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
            {
                return Results.BadRequest(new { message = ex.Message });
            }

            return Results.Ok(new { message = "アイテムを使用しました。" });
        }).RequireAuthorization();

        app.MapPost("/items/equipments/{playerEquipmentId:guid}/synthesize", async (
            ClaimsPrincipal user,
            Guid playerEquipmentId,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IEquipmentRepository equipmentRepository) =>
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

            var playerEquipments = (await playerEquipmentRepository.GetByPlayerAsync(player.Id)).ToList();
            var source = playerEquipments.FirstOrDefault(x => x.Id == new PlayerEquipmentId(playerEquipmentId));
            if (source is null || source.Status != EquipmentStatus.Inventory || source.IsBroken)
            {
                return Results.BadRequest(new { message = "合成素材として使える装備が見つかりません。" });
            }

            var equipmentMasters = await equipmentRepository.GetAllAsync();
            var equipmentById = equipmentMasters.ToDictionary(x => x.Id);
            if (!equipmentById.TryGetValue(source.EquipmentId, out var master))
            {
                return Results.BadRequest(new { message = "装備マスタが見つかりません。" });
            }

            var target = playerEquipments.FirstOrDefault(x =>
                x.Status == EquipmentStatus.Equipped &&
                x.EquipmentId == source.EquipmentId &&
                !x.IsBroken);
            if (target is null)
            {
                return Results.BadRequest(new { message = "同一装備中の回復先が見つかりません。" });
            }

            try
            {
                player.SpendGold(master.SynthesisGoldCost);
                var recovered = (int)Math.Ceiling(source.Durability / 3m);
                target.RepairDurability(recovered, master.MaxDurability, DateTimeOffset.UtcNow);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }

            var nowUtc = DateTimeOffset.UtcNow;
            await playerRepository.SaveAsync(player);
            await playerEquipmentRepository.SaveAsync(playerEquipments.Where(x => x.Id != source.Id).ToArray());
            await playerEquipmentRepository.DeleteAsync(source.Id);

            return Results.Ok(new
            {
                message = "合成しました。",
                targetPlayerEquipmentId = target.Id.Value,
                durability = target.Durability,
                maxDurability = master.MaxDurability,
                gold = player.Gold
            });
        }).RequireAuthorization();

        app.MapDelete("/items/equipments/{playerEquipmentId:guid}", async (
            ClaimsPrincipal user,
            Guid playerEquipmentId,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IItemDeletionLogRepository itemDeletionLogRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var equipment = await playerEquipmentRepository.GetAsync(new PlayerEquipmentId(playerEquipmentId));
            if (equipment is null || equipment.PlayerId != playerId.Value)
            {
                return Results.NotFound(new { message = "装備が見つかりません。" });
            }

            if (equipment.Status == EquipmentStatus.Equipped)
            {
                return Results.BadRequest(new { message = "装備中アイテムは削除できません。" });
            }

            await playerEquipmentRepository.DeleteAsync(equipment.Id);
            await itemDeletionLogRepository.AddAsync(new ItemDeletionLog(
                Guid.NewGuid(),
                playerId.Value,
                $"equipment:{equipment.EquipmentId.Value}",
                1,
                "manual_delete",
                DateTimeOffset.UtcNow));

            return Results.Ok(new { message = "装備を削除しました。" });
        }).RequireAuthorization();

        app.MapDelete("/items/stacks/{itemStackId:guid}", async (
            ClaimsPrincipal user,
            Guid itemStackId,
            [FromBody] UseItemRequest request,
            IPlayerItemStackRepository playerItemStackRepository,
            IItemDeletionLogRepository itemDeletionLogRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var stack = await playerItemStackRepository.GetAsync(new PlayerItemStackId(itemStackId));
            if (stack is null || stack.PlayerId != playerId.Value)
            {
                return Results.NotFound(new { message = "アイテムスタックが見つかりません。" });
            }

            if (request.Quantity <= 0)
            {
                return Results.BadRequest(new { message = "削除数は1以上で指定してください。" });
            }

            stack.ConsumeQuantity(request.Quantity, DateTimeOffset.UtcNow);
            if (stack.Quantity == 0)
            {
                await playerItemStackRepository.DeleteAsync(stack.Id);
            }
            else
            {
                await playerItemStackRepository.SaveAsync([stack]);
            }

            await itemDeletionLogRepository.AddAsync(new ItemDeletionLog(
                Guid.NewGuid(),
                playerId.Value,
                $"item:{stack.ItemId.Value}",
                request.Quantity,
                "manual_delete",
                DateTimeOffset.UtcNow));

            return Results.Ok(new { message = "アイテムを削除しました。" });
        }).RequireAuthorization();

        app.MapPost("/market/listings", async (
            ClaimsPrincipal user,
            CreateMarketListingRequest request,
            IDbContextFactory<AppDbContext> dbContextFactory,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IPlayerItemStackRepository playerItemStackRepository,
            IEquipmentRepository equipmentRepository,
            IItemRepository itemRepository,
            IMarketListingRepository marketListingRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            if (request.UnitPrice <= 0 || request.Quantity <= 0)
            {
                return Results.BadRequest(new { message = "販売数と単価は1以上で指定してください。" });
            }

            if (request.PlayerEquipmentId is not null)
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync();
                await using var tx = await dbContext.Database.BeginTransactionAsync();

                var equipmentEntity = await dbContext.PlayerEquipments
                    .FromSqlInterpolated($"SELECT * FROM internal.player_equipments WHERE id = {request.PlayerEquipmentId.Value} FOR UPDATE")
                    .SingleOrDefaultAsync();
                if (equipmentEntity is null || equipmentEntity.PlayerId != playerId.Value.Value)
                {
                    return Results.NotFound(new { message = "出品対象の装備が見つかりません。" });
                }

                if ((EquipmentStatus)equipmentEntity.EquipmentStatus == EquipmentStatus.Equipped || equipmentEntity.Durability <= 0)
                {
                    return Results.BadRequest(new { message = "この装備は出品できません。" });
                }

                var hasActiveListing = await dbContext.MarketListings
                    .Where(x => x.PlayerEquipmentId == equipmentEntity.Id && x.ExpiresAt > DateTimeOffset.UtcNow && x.RemainingQuantity > 0)
                    .AnyAsync();
                if (hasActiveListing)
                {
                    return Results.BadRequest(new { message = "この装備はすでに出品中です。" });
                }

                var master = await equipmentRepository.GetAsync(new EquipmentId(equipmentEntity.EquipmentId));
                if (master is null)
                {
                    return Results.BadRequest(new { message = "装備マスタが見つかりません。" });
                }

                var listedAt = DateTimeOffset.UtcNow;
                var listingId = MarketListingId.New();
                dbContext.MarketListings.Add(new MarketListingEntity
                {
                    Id = listingId.Value,
                    SellerId = playerId.Value.Value,
                    PlayerEquipmentId = equipmentEntity.Id,
                    ItemName = master.Name,
                    FlavorText = master.FlavorText,
                    Quantity = 1,
                    RemainingQuantity = 1,
                    UnitPrice = request.UnitPrice,
                    ListedAt = listedAt,
                    ExpiresAt = listedAt.AddDays(15)
                });
                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(new { message = "装備を出品しました。", listingId = listingId.Value });
            }

            if (request.ItemStackId is null)
            {
                return Results.BadRequest(new { message = "出品対象を指定してください。" });
            }

            var stack = await playerItemStackRepository.GetAsync(new PlayerItemStackId(request.ItemStackId.Value));
            if (stack is null || stack.PlayerId != playerId.Value)
            {
                return Results.NotFound(new { message = "出品対象のアイテムスタックが見つかりません。" });
            }

            if (request.Quantity > stack.Quantity)
            {
                return Results.BadRequest(new { message = "所持数を超えて出品できません。" });
            }

            var item = await itemRepository.GetAsync(stack.ItemId);
            if (item is null)
            {
                return Results.BadRequest(new { message = "アイテムマスタが見つかりません。" });
            }

            stack.ConsumeQuantity(request.Quantity, DateTimeOffset.UtcNow);
            if (stack.Quantity == 0)
            {
                await playerItemStackRepository.DeleteAsync(stack.Id);
            }
            else
            {
                await playerItemStackRepository.SaveAsync([stack]);
            }

            var itemListing = new MarketListing(
                MarketListingId.New(),
                playerId.Value,
                playerEquipmentId: null,
                stack.ItemId,
                item.Name,
                item.FlavorText,
                request.Quantity,
                request.Quantity,
                request.UnitPrice,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(15));
            await marketListingRepository.SaveAsync(itemListing);
            return Results.Ok(new { message = "アイテムを出品しました。", listingId = itemListing.Id.Value });
        }).RequireAuthorization();

        app.MapGet("/market/listings", async (
            ClaimsPrincipal user,
            IMarketListingRepository marketListingRepository,
            IPlayerRepository playerRepository,
            ITreasureMapRepository treasureMapRepository,
            IDbContextFactory<AppDbContext> dbContextFactory) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var listings = await marketListingRepository.GetActiveAsync(DateTimeOffset.UtcNow);
            var filtered = listings.Where(x => x.SellerId != playerId.Value).ToArray();
            var players = await playerRepository.GetAllAsync();
            var playerMap = players.ToDictionary(x => x.Id);
            var listingCategoryMap = await BuildListingCategoryMapAsync(filtered, treasureMapRepository, dbContextFactory);

            return Results.Ok(filtered.Select(listing =>
            {
                playerMap.TryGetValue(listing.SellerId, out var seller);
                return new
                {
                    listingId = listing.Id.Value,
                    itemName = listing.ItemName,
                    flavorText = listing.FlavorText,
                    sellerId = listing.SellerId.Value,
                    sellerName = seller?.Name ?? "Unknown",
                    sellerImagePath = seller?.ImagePath,
                    quantity = listing.RemainingQuantity,
                    unitPrice = listing.UnitPrice,
                    expiresAt = listing.ExpiresAt,
                    listingCategory = listingCategoryMap.GetValueOrDefault(listing.Id.Value, "Item")
                };
            }));
        }).RequireAuthorization();

        app.MapGet("/market/my-listings", async (
            ClaimsPrincipal user,
            IMarketListingRepository marketListingRepository,
            ITreasureMapRepository treasureMapRepository,
            IDbContextFactory<AppDbContext> dbContextFactory) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var listings = await marketListingRepository.GetBySellerAsync(playerId.Value, DateTimeOffset.UtcNow);
            var listingCategoryMap = await BuildListingCategoryMapAsync(listings, treasureMapRepository, dbContextFactory);
            return Results.Ok(listings.Select(x => new
            {
                listingId = x.Id.Value,
                itemName = x.ItemName,
                flavorText = x.FlavorText,
                quantity = x.RemainingQuantity,
                unitPrice = x.UnitPrice,
                expiresAt = x.ExpiresAt,
                listingCategory = listingCategoryMap.GetValueOrDefault(x.Id.Value, "Item")
            }));
        }).RequireAuthorization();

        app.MapPost("/market/listings/{listingId:guid}/purchase", async (
            ClaimsPrincipal user,
            Guid listingId,
            PurchaseMarketListingRequest request,
            IDbContextFactory<AppDbContext> dbContextFactory,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IPlayerItemStackRepository playerItemStackRepository,
            IItemRepository itemRepository,
            IEquipmentRepository equipmentRepository,
            IMarketTradeHistoryRepository marketTradeHistoryRepository,
            ChatService chatService) =>
        {
            var buyerId = EndpointHelpers.TryGetPlayerId(user);
            if (buyerId is null)
            {
                return Results.Unauthorized();
            }

            if (request.Quantity <= 0)
            {
                return Results.BadRequest(new { message = "購入数は1以上で指定してください。" });
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await using var tx = await dbContext.Database.BeginTransactionAsync();

            var lockedListingEntity = await dbContext.MarketListings
                .FromSqlInterpolated($"SELECT * FROM internal.market_listings WHERE id = {listingId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (lockedListingEntity is null)
            {
                return Results.NotFound(new { message = "出品が見つかりません。" });
            }

            var listing = new MarketListing(
                new MarketListingId(lockedListingEntity.Id),
                new PlayerId(lockedListingEntity.SellerId),
                lockedListingEntity.PlayerEquipmentId is null ? null : new PlayerEquipmentId(lockedListingEntity.PlayerEquipmentId.Value),
                lockedListingEntity.ItemId is null ? null : new ItemId(lockedListingEntity.ItemId.Value),
                lockedListingEntity.ItemName,
                lockedListingEntity.FlavorText,
                lockedListingEntity.Quantity,
                lockedListingEntity.RemainingQuantity,
                lockedListingEntity.UnitPrice,
                lockedListingEntity.ListedAt,
                lockedListingEntity.ExpiresAt);

            if (listing.SellerId == buyerId.Value)
            {
                return Results.BadRequest(new { message = "自分の出品は購入できません。" });
            }

            if (listing.IsExpired(DateTimeOffset.UtcNow))
            {
                return Results.UnprocessableEntity(new { message = "期限切れの出品です。" });
            }

            if (request.Quantity > listing.RemainingQuantity)
            {
                return Results.Conflict(new { message = "出品残数が不足しています。" });
            }

            var buyer = await playerRepository.GetPlayerAsync(buyerId.Value);
            var seller = await playerRepository.GetPlayerAsync(listing.SellerId);
            if (buyer is null || seller is null)
            {
                return Results.NotFound(new { message = "プレイヤーが見つかりません。" });
            }

            var price = checked(listing.UnitPrice * request.Quantity);
            try
            {
                buyer.SpendGold(price);
                seller.GainGold(price);
            }
            catch (InvalidOperationException ex)
            {
                return Results.UnprocessableEntity(new { message = ex.Message });
            }

            if (listing.PlayerEquipmentId is not null)
            {
                if (request.Quantity != 1)
                {
                    return Results.BadRequest(new { message = "装備は1件ずつのみ購入できます。" });
                }

                var buyerEquipments = await playerEquipmentRepository.GetByPlayerAsync(buyer.Id);
                var buyerStacks = await playerItemStackRepository.GetByPlayerAsync(buyer.Id);
                var buyerListings = await dbContext.MarketListings
                    .AsNoTracking()
                    .Where(x => x.SellerId == buyer.Id.Value && x.ExpiresAt > DateTimeOffset.UtcNow && x.RemainingQuantity > 0)
                    .ToListAsync();
                var buyerListedEquipmentIds = buyerListings
                    .Where(x => x.PlayerEquipmentId is not null)
                    .Select(x => new PlayerEquipmentId(x.PlayerEquipmentId!.Value))
                    .ToHashSet();
                var buyerUsedSlots = CalculateUsedSlots(buyerEquipments, buyerStacks, buyerListedEquipmentIds);
                if (buyerUsedSlots + 1 > ItemCapacity)
                {
                    return Results.UnprocessableEntity(new { message = "所持枠が不足しています。" });
                }

                var equipment = await playerEquipmentRepository.GetAsync(listing.PlayerEquipmentId.Value);
                if (equipment is null)
                {
                    return Results.NotFound(new { message = "装備個体が見つかりません。" });
                }

                if (equipment.PlayerId != listing.SellerId)
                {
                    return Results.Conflict(new { message = "出品中の装備所有者が一致しません。" });
                }

                equipment.TransferOwnership(buyer.Id, DateTimeOffset.UtcNow);
                await playerEquipmentRepository.SaveAsync([equipment]);
            }
            else
            {
                if (listing.ItemId is null)
                {
                    return Results.BadRequest(new { message = "アイテム出品情報が不正です。" });
                }

                var item = await itemRepository.GetAsync(listing.ItemId.Value);
                if (item is null)
                {
                    return Results.BadRequest(new { message = "アイテムマスタが見つかりません。" });
                }

                var buyerStacks = (await playerItemStackRepository.GetByPlayerAsync(buyer.Id)).ToList();
                var existingStack = buyerStacks.FirstOrDefault(x => x.ItemId == item.Id);
                if (existingStack is null)
                {
                    var buyerEquipments = await playerEquipmentRepository.GetByPlayerAsync(buyer.Id);
                    var buyerListings = await dbContext.MarketListings
                        .AsNoTracking()
                        .Where(x => x.SellerId == buyer.Id.Value && x.ExpiresAt > DateTimeOffset.UtcNow && x.RemainingQuantity > 0)
                        .ToListAsync();
                    var buyerListedEquipmentIds = buyerListings
                        .Where(x => x.PlayerEquipmentId is not null)
                        .Select(x => new PlayerEquipmentId(x.PlayerEquipmentId!.Value))
                        .ToHashSet();
                    var buyerUsedSlots = CalculateUsedSlots(buyerEquipments, buyerStacks, buyerListedEquipmentIds);
                    if (buyerUsedSlots + 1 > ItemCapacity)
                    {
                        return Results.UnprocessableEntity(new { message = "所持枠が不足しています。" });
                    }

                    existingStack = new PlayerItemStack(PlayerItemStackId.New(), buyer.Id, item.Id, request.Quantity, DateTimeOffset.UtcNow);
                    buyerStacks.Add(existingStack);
                }
                else
                {
                    existingStack.AddQuantity(request.Quantity, item.MaxStack, DateTimeOffset.UtcNow);
                }

                await playerItemStackRepository.SaveAsync(buyerStacks);
            }

            listing.Purchase(request.Quantity);
            if (listing.IsSoldOut)
            {
                dbContext.MarketListings.Remove(lockedListingEntity);
            }
            else
            {
                lockedListingEntity.RemainingQuantity = listing.RemainingQuantity;
            }

            await dbContext.SaveChangesAsync();
            await playerRepository.SaveAsync(buyer);
            await playerRepository.SaveAsync(seller);
            await marketTradeHistoryRepository.AddAsync(new MarketTradeHistory(
                Guid.NewGuid(),
                seller.Id,
                buyer.Id,
                listing.PlayerEquipmentId is not null
                    ? $"equipment:{listing.PlayerEquipmentId.Value.Value}"
                    : $"item:{listing.ItemId!.Value.Value}",
                request.Quantity,
                listing.UnitPrice,
                DateTimeOffset.UtcNow));
            await chatService.PostSystemMessageAsync(seller.Id, $"{listing.ItemName} x{request.Quantity} が {price} Gold で売れました。");
            await tx.CommitAsync();

            return Results.Ok(new { message = "購入しました。", gold = buyer.Gold });
        }).RequireAuthorization();

        app.MapPost("/internal/market/listings/cleanup-expired", async (
            HttpRequest request,
            IConfiguration configuration,
            MarketListingCleanupService marketListingCleanupService) =>
        {
            var expectedToken = configuration["Maintenance:MarketCleanupToken"];
            if (string.IsNullOrWhiteSpace(expectedToken))
            {
                return Results.Problem(
                    detail: "Maintenance:MarketCleanupToken が設定されていません。",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var providedToken = request.Headers[MaintenanceTokenHeaderName].ToString();
            if (!SecureEquals(providedToken, expectedToken))
            {
                return Results.Unauthorized();
            }

            var result = await marketListingCleanupService.DeleteExpiredAsync(DateTimeOffset.UtcNow);
            return Results.Ok(new
            {
                message = "期限切れ出品を削除しました。",
                deletedListings = result.DeletedListings,
                deletedEquipments = result.DeletedEquipments,
                deletedItemQuantity = result.DeletedItemQuantity
            });
        }).ExcludeFromDescription();

        app.MapPost("/internal/development/cleanup-game-data", async (
            HttpRequest request,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            DevelopmentDataCleanupService developmentDataCleanupService) =>
        {

            if (!IsLocalDevelopmentRequest(request))
            {
                var expectedToken = configuration["Maintenance:MarketCleanupToken"];
                if (string.IsNullOrWhiteSpace(expectedToken))
                {
                    return Results.Problem(
                        detail: "Maintenance:MarketCleanupToken が設定されていません。",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                var providedToken = request.Headers[MaintenanceTokenHeaderName].ToString();
                if (!SecureEquals(providedToken, expectedToken))
                {
                    return Results.Unauthorized();
                }
            }

            var result = await developmentDataCleanupService.CleanupAsync();
            return Results.Ok(new
            {
                message = "開発用ゲームデータを削除しました。",
                deletedPlayers = result.DeletedPlayers,
                deletedChatRooms = result.DeletedChatRooms,
                deletedChatMessages = result.DeletedChatMessages,
                deletedPlayerMoves = result.DeletedPlayerMoves,
                deletedPlayerMasterJobs = result.DeletedPlayerMasterJobs,
                deletedPlayerEquipments = result.DeletedPlayerEquipments,
                deletedPlayerItemStacks = result.DeletedPlayerItemStacks,
                deletedMarketListings = result.DeletedMarketListings,
                deletedMarketTradeHistories = result.DeletedMarketTradeHistories,
                deletedItemDeletionLogs = result.DeletedItemDeletionLogs,
                deletedQuestRooms = result.DeletedQuestRooms,
                deletedQuestRoomParticipants = result.DeletedQuestRoomParticipants,
                deletedQuestRuns = result.DeletedQuestRuns,
                deletedQuestRunPartySnapshots = result.DeletedQuestRunPartySnapshots,
                deletedQuestRunPartyMembers = result.DeletedQuestRunPartyMembers,
                deletedQuestRunEnemies = result.DeletedQuestRunEnemies,
                deletedQuestTurnCommands = result.DeletedQuestTurnCommands,
                deletedQuestFloorTraps = result.DeletedQuestFloorTraps,
                deletedQuestRewardSummaries = result.DeletedQuestRewardSummaries
            });
        }).ExcludeFromDescription();

        return app;
    }

    private static int CalculateUsedSlots(
        IReadOnlyList<PlayerEquipment> playerEquipments,
        IReadOnlyList<PlayerItemStack> itemStacks,
        IReadOnlySet<PlayerEquipmentId> listedEquipmentIds)
    {
        return playerEquipments.Count(x => x.Status != EquipmentStatus.Equipped && !listedEquipmentIds.Contains(x.Id))
               + itemStacks.Count;
    }

    private static object? ToEquipmentView(PlayerEquipment playerEquipment, IReadOnlyDictionary<EquipmentId, Equipment> equipmentById)
    {
        if (!equipmentById.TryGetValue(playerEquipment.EquipmentId, out var equipment))
        {
            return null;
        }

        return new
        {
            kind = "equipment",
            playerEquipmentId = playerEquipment.Id.Value,
            equipmentId = equipment.Id.Value,
            name = equipment.Name,
            flavorText = equipment.FlavorText,
            equipmentType = equipment.Type.ToString(),
            status = playerEquipment.Status.ToString(),
            durability = playerEquipment.Durability,
            maxDurability = equipment.MaxDurability,
            mastery = playerEquipment.Mastery,
            masteryCap = equipment.MasteryCap,
            synthesisGoldCost = equipment.SynthesisGoldCost,
            statusBonus = new
            {
                maxHp = equipment.BonusValues.MaxHp,
                maxMp = equipment.BonusValues.MaxMp,
                strength = equipment.BonusValues.Strength,
                defense = equipment.BonusValues.Defense,
                intelligence = equipment.BonusValues.Intelligence,
                luck = equipment.BonusValues.Luck,
                speed = equipment.BonusValues.Speed
            }
        };
    }

    private static object? ToItemStackView(
        PlayerItemStack stack,
        IReadOnlyDictionary<ItemId, Item> itemById,
        IReadOnlySet<int> treasureMapItemIds)
    {
        if (!itemById.TryGetValue(stack.ItemId, out var item))
        {
            return null;
        }

        return new
        {
            kind = "item",
            itemStackId = stack.Id.Value,
            itemId = item.Id.Value,
            name = item.Name,
            flavorText = item.FlavorText,
            quantity = stack.Quantity,
            canUseFromInventory = !treasureMapItemIds.Contains(item.Id.Value),
            effectType = item.EffectType.ToString(),
            requiredLevel = item.RequiredLevel,
            changeJobTo = item.ChangeJobTo?.ToString(),
            statusBonus = item.StatusBonus is null
                ? null
                : new
                {
                    maxHp = item.StatusBonus.MaxHp,
                    maxMp = item.StatusBonus.MaxMp,
                    strength = item.StatusBonus.Strength,
                    defense = item.StatusBonus.Defense,
                    intelligence = item.StatusBonus.Intelligence,
                    luck = item.StatusBonus.Luck,
                    speed = item.StatusBonus.Speed
                },
            statusBonusPercent = item.StatusBonusPercent is null
                ? null
                : new
                {
                    maxHp = item.StatusBonusPercent.MaxHpPercent,
                    maxMp = item.StatusBonusPercent.MaxMpPercent,
                    strength = item.StatusBonusPercent.StrengthPercent,
                    defense = item.StatusBonusPercent.DefensePercent,
                    intelligence = item.StatusBonusPercent.IntelligencePercent,
                    luck = item.StatusBonusPercent.LuckPercent,
                    speed = item.StatusBonusPercent.SpeedPercent
                }
        };
    }

    private static async Task<bool> IsTreasureMapItemAsync(ItemId itemId, ITreasureMapRepository treasureMapRepository)
    {
        var treasureMapItemIds = await ResolveTreasureMapItemIdsAsync(treasureMapRepository);
        return treasureMapItemIds.Contains(itemId.Value);
    }

    private static async Task<HashSet<int>> ResolveTreasureMapItemIdsAsync(ITreasureMapRepository treasureMapRepository)
    {
        var maps = await treasureMapRepository.GetAllAsync();
        var ids = new HashSet<int>();

        foreach (var map in maps)
        {
            if (int.TryParse(map.Code, out var itemId))
            {
                ids.Add(itemId);
            }
            else
            {
                ids.Add(map.Id.Value);
            }
        }

        return ids;
    }

    private static async Task<IReadOnlyDictionary<Guid, string>> BuildListingCategoryMapAsync(
        IReadOnlyList<MarketListing> listings,
        ITreasureMapRepository treasureMapRepository,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        var treasureMapItemIds = await ResolveTreasureMapItemIdsAsync(treasureMapRepository);
        var equipmentIds = listings
            .Where(x => x.PlayerEquipmentId is not null)
            .Select(x => x.PlayerEquipmentId!.Value.Value)
            .ToArray();

        var equipmentTypeById = new Dictionary<Guid, int>();
        if (equipmentIds.Length > 0)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            equipmentTypeById = await dbContext.PlayerEquipments
                .AsNoTracking()
                .Where(x => equipmentIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.EquipmentType);
        }

        return listings.ToDictionary(
            x => x.Id.Value,
            x =>
            {
                if (x.PlayerEquipmentId is not null && equipmentTypeById.TryGetValue(x.PlayerEquipmentId.Value.Value, out var equipmentType))
                {
                    return (EquipmentType)equipmentType == EquipmentType.Weapon
                        ? "Weapon"
                        : "Armor";
                }

                return x.ItemId is not null && treasureMapItemIds.Contains(x.ItemId.Value.Value)
                    ? "Map"
                    : "Item";
            });
    }

    private static bool SecureEquals(string actual, string expected)
    {
        if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        if (actualBytes.Length != expectedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    private static bool IsLocalDevelopmentRequest(HttpRequest request)
    {
        var remoteIp = request.HttpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return false;
        }

        return IPAddress.IsLoopback(remoteIp);
    }
}
