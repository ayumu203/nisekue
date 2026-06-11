using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using server.application.chat;
using server.application.player;
using server.domain;
using server.domain.move;
using server.domain.player;
using server.domain.treasuremap;
using server.infrastructure;
using server.infrastructure.player;

namespace server.endpoints;

internal static class ItemEndpoints
{
    private const int ItemCapacity = 20;

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
            IPlayerMutationService playerMutationService,
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

            var stack = await playerItemStackRepository.GetAsync(new PlayerItemStackId(itemStackId));
            if (stack is null || stack.PlayerId != playerId.Value)
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

            stack.ConsumeQuantity(request.Quantity, DateTimeOffset.UtcNow);

            await playerMutationService.MutateAsync(playerId.Value, async player =>
            {
                if (!item.CanUse(player))
                {
                    throw new DomainException("このアイテムを使用する条件を満たしていません。");
                }

                try
                {
                    switch (item.EffectType)
                    {
                        case ItemEffectType.StatBoost:
                            player.UpdateStatus(itemStatBoostService.Apply(player.Status, item, request.Quantity));
                            break;
                        case ItemEffectType.ChangeJob:
                            {
                                if (request.Quantity != 1)
                                {
                                    throw new DomainException("転職アイテムは1個ずつのみ使用できます。");
                                }

                                if (item.ChangeJobTo is null)
                                {
                                    throw new DomainException("転職先ジョブが定義されていません。");
                                }

                                var jobProfile = jobProfileRepository.GetByJob(item.ChangeJobTo.Value);
                                var learningRule = jobMoveLearningRuleRepository.GetByJob(item.ChangeJobTo.Value);
                                player.ChangeJob(item.ChangeJobTo.Value, jobProfile, learningRule, ignoreRequirements: true);
                                break;
                            }
                        case ItemEffectType.ExpMultiplier:
                            {
                                if (request.Quantity != 1)
                                {
                                    throw new DomainException("経験値倍率アイテムは1個ずつのみ使用できます。");
                                }

                                if (item.ExpMultiplier is null)
                                {
                                    throw new DomainException("経験値倍率が定義されていません。");
                                }

                                if (player.HasAnyExpMultiplierFlag())
                                {
                                    throw new DomainException("すでに経験値倍率が設定されています。効果が切れてから使用してください。");
                                }

                                var flag = ExpMultiplierFlag.ToFlag(item.ExpMultiplier.Value);
                                player.SetExpMultiplierFlag(flag);
                                break;
                            }
                        case ItemEffectType.UnlockMap:
                            {
                                if (request.Quantity != 1)
                                {
                                    throw new DomainException("マップ解放アイテムは1個ずつのみ使用できます。");
                                }

                                if (item.MapUnlockFlag is null)
                                {
                                    throw new DomainException("マップ解放フラグが定義されていません。");
                                }

                                player.SetMapUnlockFlag(item.MapUnlockFlag.Value);
                                break;
                            }
                        default:
                            throw new DomainException("未対応のアイテム効果です。");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    throw new DomainException(ex.Message, ex);
                }

                return 0;
            });

            if (stack.Quantity == 0)
            {
                await playerItemStackRepository.DeleteAsync(stack.Id);
            }
            else
            {
                await playerItemStackRepository.SaveAsync([stack]);
            }

            return Results.Ok(new { message = "アイテムを使用しました。" });
        }).RequireAuthorization();

        app.MapPost("/items/equipments/{targetId:guid}/synthesize", async (
            ClaimsPrincipal user,
            Guid targetId,
            SynthesizeEquipmentRequest request,
            IDbContextFactory<AppDbContext> dbContextFactory,
            PlayerForUpdateLockService playerForUpdateLockService,
            IMemoryCache cache,
            IEquipmentRepository equipmentRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var equipmentMasters = await equipmentRepository.GetAllAsync();
            var equipmentById = equipmentMasters.ToDictionary(x => x.Id);

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await using var tx = await dbContext.Database.BeginTransactionAsync();

            var lockedPlayers = await playerForUpdateLockService.LockPlayersAsync(dbContext, [playerId.Value.Value]);

            var targetEntity = await dbContext.PlayerEquipments
                .FromSqlInterpolated($"SELECT * FROM internal.player_equipments WHERE id = {targetId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (targetEntity is null || targetEntity.PlayerId != playerId.Value.Value)
            {
                return Results.BadRequest(new { message = "合成対象の装備が見つかりません。" });
            }

            var sourceEntity = await dbContext.PlayerEquipments
                .FromSqlInterpolated($"SELECT * FROM internal.player_equipments WHERE id = {request.SourcePlayerEquipmentId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (sourceEntity is null || sourceEntity.PlayerId != playerId.Value.Value)
            {
                return Results.BadRequest(new { message = "合成素材として使える装備が見つかりません。" });
            }

            var target = PlayerEquipmentEntityMapper.MapToDomain(targetEntity);
            var source = PlayerEquipmentEntityMapper.MapToDomain(sourceEntity);
            if (!equipmentById.TryGetValue(target.EquipmentId, out var master))
            {
                return Results.BadRequest(new { message = "装備マスタが見つかりません。" });
            }

            var goldCost = master.SynthesisGoldCost * (target.PlusValue + 1);
            var playerEntity = lockedPlayers[playerId.Value.Value];
            var player = PlayerEntityMapper.MapToDomain(playerEntity, moveEntity: null);
            try
            {
                target.Synthesize(source, goldCost, DateTimeOffset.UtcNow);
                player.SpendGold(goldCost);
            }
            catch (InvalidOperationException ex)
            {
                throw new DomainException(ex.Message, ex);
            }

            playerEntity.Gold = player.Gold;

            PlayerEquipmentEntityMapper.ApplyEntity(targetEntity, target);
            dbContext.PlayerEquipments.Remove(sourceEntity);
            await dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            cache.Remove(server.shared.constants.player.PlayerCacheConstants.PlayerKey(playerEntity.Id));
            cache.Remove(server.shared.constants.player.PlayerCacheConstants.AllPlayersKey);
            cache.Remove(DbPlayerEquipmentRepository.EquippedCacheKey(playerEntity.Id));

            return Results.Ok(new
            {
                message = "合成しました。",
                targetPlayerEquipmentId = target.Id.Value,
                plusValue = target.PlusValue,
                gold = playerEntity.Gold
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

                if ((EquipmentStatus)equipmentEntity.EquipmentStatus == EquipmentStatus.Equipped || (EquipmentStatus)equipmentEntity.EquipmentStatus == EquipmentStatus.Broken)
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
            IEquipmentRepository equipmentRepository,
            IDbContextFactory<AppDbContext> dbContextFactory) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var filtered = await marketListingRepository.GetActiveAsync(DateTimeOffset.UtcNow, playerId.Value);
            var players = await playerRepository.GetAllAsync();
            var playerMap = players.ToDictionary(x => x.Id);
            var playerEquipmentSnapshotMap = await LoadListedPlayerEquipmentSnapshotMapAsync(filtered, dbContextFactory);
            var listingCategoryMap = await BuildListingCategoryMapAsync(filtered, treasureMapRepository, playerEquipmentSnapshotMap);
            var equipmentDetailMap = await BuildEquipmentMarketDetailMapAsync(filtered, equipmentRepository, playerEquipmentSnapshotMap);

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
                    listingCategory = listingCategoryMap.GetValueOrDefault(listing.Id.Value, "Item"),
                    equipmentDetail = equipmentDetailMap.GetValueOrDefault(listing.Id.Value)
                };
            }));
        }).RequireAuthorization();

        app.MapGet("/market/my-listings", async (
            ClaimsPrincipal user,
            IMarketListingRepository marketListingRepository,
            ITreasureMapRepository treasureMapRepository,
            IEquipmentRepository equipmentRepository,
            IDbContextFactory<AppDbContext> dbContextFactory) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var listings = await marketListingRepository.GetBySellerAsync(playerId.Value, DateTimeOffset.UtcNow);
            var playerEquipmentSnapshotMap = await LoadListedPlayerEquipmentSnapshotMapAsync(listings, dbContextFactory);
            var listingCategoryMap = await BuildListingCategoryMapAsync(listings, treasureMapRepository, playerEquipmentSnapshotMap);
            var equipmentDetailMap = await BuildEquipmentMarketDetailMapAsync(listings, equipmentRepository, playerEquipmentSnapshotMap);
            return Results.Ok(listings.Select(x => new
            {
                listingId = x.Id.Value,
                itemName = x.ItemName,
                flavorText = x.FlavorText,
                quantity = x.RemainingQuantity,
                unitPrice = x.UnitPrice,
                expiresAt = x.ExpiresAt,
                listingCategory = listingCategoryMap.GetValueOrDefault(x.Id.Value, "Item"),
                equipmentDetail = equipmentDetailMap.GetValueOrDefault(x.Id.Value)
            }));
        }).RequireAuthorization();

        app.MapPost("/market/listings/{listingId:guid}/purchase", async (
            ClaimsPrincipal user,
            Guid listingId,
            PurchaseMarketListingRequest request,
            IDbContextFactory<AppDbContext> dbContextFactory,
            PlayerForUpdateLockService playerForUpdateLockService,
            IMemoryCache cache,
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

            var lockedPlayerIds = new[] { buyerId.Value.Value, listing.SellerId.Value }
                .Distinct()
                .ToArray();
            IReadOnlyDictionary<Guid, PlayerEntity> lockedPlayers;
            try
            {
                lockedPlayers = await playerForUpdateLockService.LockPlayersAsync(dbContext, lockedPlayerIds);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }

            var buyer = lockedPlayers[buyerId.Value.Value];
            var seller = lockedPlayers[listing.SellerId.Value];

            var price = checked(listing.UnitPrice * request.Quantity);
            try
            {
                var buyerDomain = PlayerEntityMapper.MapToDomain(buyer, moveEntity: null);
                var sellerDomain = PlayerEntityMapper.MapToDomain(seller, moveEntity: null);
                buyerDomain.SpendGold(price);
                sellerDomain.GainGold(price);
                buyer.Gold = buyerDomain.Gold;
                seller.Gold = sellerDomain.Gold;
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

                var buyerEquipments = await playerEquipmentRepository.GetByPlayerAsync(new PlayerId(buyer.Id));
                var buyerStacks = await playerItemStackRepository.GetByPlayerAsync(new PlayerId(buyer.Id));
                var buyerListings = await dbContext.MarketListings
                    .AsNoTracking()
                    .Where(x => x.SellerId == buyer.Id && x.ExpiresAt > DateTimeOffset.UtcNow && x.RemainingQuantity > 0)
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

                equipment.TransferOwnership(new PlayerId(buyer.Id), DateTimeOffset.UtcNow);
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

                var buyerStacks = (await playerItemStackRepository.GetByPlayerAsync(new PlayerId(buyer.Id))).ToList();
                var existingStack = buyerStacks.FirstOrDefault(x => x.ItemId == item.Id);
                if (existingStack is null)
                {
                    var buyerEquipments = await playerEquipmentRepository.GetByPlayerAsync(new PlayerId(buyer.Id));
                    var buyerListings = await dbContext.MarketListings
                        .AsNoTracking()
                        .Where(x => x.SellerId == buyer.Id && x.ExpiresAt > DateTimeOffset.UtcNow && x.RemainingQuantity > 0)
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

                    existingStack = new PlayerItemStack(PlayerItemStackId.New(), new PlayerId(buyer.Id), item.Id, request.Quantity, DateTimeOffset.UtcNow);
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
            await marketTradeHistoryRepository.AddAsync(new MarketTradeHistory(
                Guid.NewGuid(),
                new PlayerId(seller.Id),
                new PlayerId(buyer.Id),
                listing.PlayerEquipmentId is not null
                    ? $"equipment:{listing.PlayerEquipmentId.Value.Value}"
                    : $"item:{listing.ItemId!.Value.Value}",
                request.Quantity,
                listing.UnitPrice,
                DateTimeOffset.UtcNow));
            await tx.CommitAsync();

            cache.Remove(server.shared.constants.player.PlayerCacheConstants.PlayerKey(buyer.Id));
            cache.Remove(server.shared.constants.player.PlayerCacheConstants.PlayerKey(seller.Id));
            cache.Remove(server.shared.constants.player.PlayerCacheConstants.AllPlayersKey);
            await chatService.TryPostSystemMessageAsync(
                new PlayerId(seller.Id),
                $"{listing.ItemName} x{request.Quantity} が {price} Gold で売れました。",
                "マーケット購入");

            return Results.Ok(new { message = "購入しました。", gold = buyer.Gold });
        }).RequireAuthorization();

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
            plusValue = playerEquipment.PlusValue,
            mastery = playerEquipment.Mastery,
            masteryCap = equipment.MasteryCap,
            synthesisGoldCost = equipment.SynthesisGoldCost,
            statusBonus = ToStatusBonusView(equipment.BonusValues)
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
            expMultiplier = item.ExpMultiplier,
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

    private static async Task<IReadOnlyDictionary<Guid, ListedPlayerEquipmentSnapshot>> LoadListedPlayerEquipmentSnapshotMapAsync(
        IReadOnlyList<MarketListing> listings,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        var playerEquipmentIds = listings
            .Where(x => x.PlayerEquipmentId is not null)
            .Select(x => x.PlayerEquipmentId!.Value.Value)
            .Distinct()
            .ToArray();

        if (playerEquipmentIds.Length == 0)
        {
            return new Dictionary<Guid, ListedPlayerEquipmentSnapshot>();
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.PlayerEquipments
            .AsNoTracking()
            .Where(x => playerEquipmentIds.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => new ListedPlayerEquipmentSnapshot(
                    x.EquipmentId,
                    (EquipmentType)x.EquipmentType,
                    x.Durability,
                    x.Mastery,
                    x.PlusValue));
    }

    private static async Task<IReadOnlyDictionary<Guid, string>> BuildListingCategoryMapAsync(
        IReadOnlyList<MarketListing> listings,
        ITreasureMapRepository treasureMapRepository,
        IReadOnlyDictionary<Guid, ListedPlayerEquipmentSnapshot> playerEquipmentSnapshotMap)
    {
        var treasureMapItemIds = await ResolveTreasureMapItemIdsAsync(treasureMapRepository);

        return listings.ToDictionary(
            x => x.Id.Value,
            x =>
            {
                if (x.PlayerEquipmentId is not null
                    && playerEquipmentSnapshotMap.TryGetValue(x.PlayerEquipmentId.Value.Value, out var playerEquipmentSnapshot))
                {
                    return playerEquipmentSnapshot.EquipmentType == EquipmentType.Weapon
                        ? "Weapon"
                        : "Armor";
                }

                return x.ItemId is not null && treasureMapItemIds.Contains(x.ItemId.Value.Value)
                    ? "Map"
                    : "Item";
            });
    }

    private static async Task<IReadOnlyDictionary<Guid, EquipmentMarketDetailView>> BuildEquipmentMarketDetailMapAsync(
        IReadOnlyList<MarketListing> listings,
        IEquipmentRepository equipmentRepository,
        IReadOnlyDictionary<Guid, ListedPlayerEquipmentSnapshot> playerEquipmentSnapshotMap)
    {
        if (playerEquipmentSnapshotMap.Count == 0)
        {
            return new Dictionary<Guid, EquipmentMarketDetailView>();
        }

        var equipmentById = (await equipmentRepository.GetAllAsync()).ToDictionary(x => x.Id);
        var details = new Dictionary<Guid, EquipmentMarketDetailView>();

        foreach (var listing in listings)
        {
            if (listing.PlayerEquipmentId is null)
            {
                continue;
            }

            var playerEquipmentId = listing.PlayerEquipmentId.Value.Value;
            if (!playerEquipmentSnapshotMap.TryGetValue(playerEquipmentId, out var playerEquipmentSnapshot))
            {
                continue;
            }

            var equipmentId = new EquipmentId(playerEquipmentSnapshot.EquipmentId);
            if (!equipmentById.TryGetValue(equipmentId, out var equipment))
            {
                continue;
            }

            details[listing.Id.Value] = new EquipmentMarketDetailView(
                equipment.Type.ToString(),
                playerEquipmentSnapshot.PlusValue,
                playerEquipmentSnapshot.Mastery,
                equipment.MasteryCap,
                ToStatusBonusView(equipment.BonusValues));
        }

        return details;
    }

    private static StatusBonusView ToStatusBonusView(EquipmentStatusBonus bonus)
    {
        return new StatusBonusView(
            bonus.MaxHp,
            bonus.MaxMp,
            bonus.Strength,
            bonus.Defense,
            bonus.Intelligence,
            bonus.Luck,
            bonus.Speed);
    }

    private sealed record ListedPlayerEquipmentSnapshot(
        int EquipmentId,
        EquipmentType EquipmentType,
        int Durability,
        int Mastery,
        int PlusValue);

    private sealed record EquipmentMarketDetailView(
        string EquipmentType,
        int PlusValue,
        int Mastery,
        int MasteryCap,
        StatusBonusView StatusBonus);

    private sealed record StatusBonusView(
        int MaxHp,
        int MaxMp,
        int Strength,
        int Defense,
        int Intelligence,
        int Luck,
        int Speed);
}
