using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using server.domain;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;

namespace server.application.player;

public sealed class MarketPurchaseService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    PlayerForUpdateLockService playerForUpdateLockService,
    IItemRepository itemRepository)
{
    public async Task<PurchaseMarketListingResult> PurchaseAsync(PlayerId buyerId, MarketListingId listingId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status400BadRequest, "購入数は1以上で指定してください。");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;

        var now = DateTimeOffset.UtcNow;
        var lockedListingEntity = await LockMarketListingAsync(dbContext, listingId.Value);
        if (lockedListingEntity is null)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status404NotFound, "出品が見つかりません。");
        }

        var listing = MarketListingEntityMapper.MapToDomain(lockedListingEntity);
        if (listing.SellerId == buyerId)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status400BadRequest, "自分の出品は購入できません。");
        }

        if (listing.IsExpired(now))
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status422UnprocessableEntity, "期限切れの出品です。");
        }

        if (quantity > listing.RemainingQuantity)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status409Conflict, "出品残数が不足しています。");
        }

        var lockedPlayers = await playerForUpdateLockService.LockPlayersAsync(dbContext, [buyerId.Value, listing.SellerId.Value]);
        var buyer = lockedPlayers[buyerId.Value];
        var seller = lockedPlayers[listing.SellerId.Value];

        var totalPrice = checked(listing.UnitPrice * quantity);
        ApplyGoldTransfer(buyer, seller, totalPrice);

        if (listing.PlayerEquipmentId is not null)
        {
            await HandleEquipmentPurchaseAsync(dbContext, listing, buyer.Id, quantity, now);
        }
        else
        {
            await HandleItemPurchaseAsync(dbContext, listing, buyer.Id, quantity, now);
        }

        listing.Purchase(quantity);
        if (listing.IsSoldOut)
        {
            dbContext.MarketListings.Remove(lockedListingEntity);
        }
        else
        {
            lockedListingEntity.RemainingQuantity = listing.RemainingQuantity;
        }

        dbContext.MarketTradeHistories.Add(new MarketTradeHistoryEntity
        {
            Id = Guid.NewGuid(),
            SellerId = seller.Id,
            BuyerId = buyer.Id,
            ItemIdentifier = listing.PlayerEquipmentId is not null
                ? $"equipment:{listing.PlayerEquipmentId.Value.Value}"
                : $"item:{listing.ItemId!.Value.Value}",
            Quantity = quantity,
            UnitPrice = listing.UnitPrice,
            PurchasedAt = now
        });

        await dbContext.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return new PurchaseMarketListingResult(
            buyer.Id,
            seller.Id,
            listing.ItemName,
            quantity,
            totalPrice,
            buyer.Gold);
    }

    private static void ApplyGoldTransfer(PlayerEntity buyerEntity, PlayerEntity sellerEntity, int totalPrice)
    {
        try
        {
            var buyer = PlayerEntityMapper.MapToDomain(buyerEntity, moveEntity: null);
            var seller = PlayerEntityMapper.MapToDomain(sellerEntity, moveEntity: null);
            buyer.SpendGold(totalPrice);
            seller.GainGold(totalPrice);
            buyerEntity.Gold = buyer.Gold;
            sellerEntity.Gold = seller.Gold;
        }
        catch (InvalidOperationException ex)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status422UnprocessableEntity, ex.Message, ex);
        }
    }

    private async Task HandleEquipmentPurchaseAsync(
        AppDbContext dbContext,
        MarketListing listing,
        Guid buyerId,
        int quantity,
        DateTimeOffset now)
    {
        if (quantity != 1)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status400BadRequest, "装備は1件ずつのみ購入できます。");
        }

        if (await CalculateUsedSlotsForPlayerAsync(dbContext, buyerId, now) + 1 > 20)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status422UnprocessableEntity, "所持枠が不足しています。");
        }

        var equipmentEntity = await LockPlayerEquipmentAsync(dbContext, listing.PlayerEquipmentId!.Value.Value);
        if (equipmentEntity is null)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status404NotFound, "装備個体が見つかりません。");
        }

        if (equipmentEntity.PlayerId != listing.SellerId.Value)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status409Conflict, "出品中の装備所有者が一致しません。");
        }

        var equipment = PlayerEquipmentEntityMapper.MapToDomain(equipmentEntity);
        equipment.TransferOwnership(new PlayerId(buyerId), now);
        PlayerEquipmentEntityMapper.ApplyEntity(equipmentEntity, equipment);
    }

    private async Task HandleItemPurchaseAsync(
        AppDbContext dbContext,
        MarketListing listing,
        Guid buyerId,
        int quantity,
        DateTimeOffset now)
    {
        if (listing.ItemId is null)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status400BadRequest, "アイテム出品情報が不正です。");
        }

        var item = await itemRepository.GetAsync(listing.ItemId.Value);
        if (item is null)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status400BadRequest, "アイテムマスタが見つかりません。");
        }

        var existingStackEntity = await FindPlayerItemStackAsync(dbContext, buyerId, item.Id.Value);
        if (existingStackEntity is null)
        {
            if (await CalculateUsedSlotsForPlayerAsync(dbContext, buyerId, now) + 1 > 20)
            {
                throw new MarketPurchaseFailedException(StatusCodes.Status422UnprocessableEntity, "所持枠が不足しています。");
            }

            dbContext.PlayerItemStacks.Add(new PlayerItemStackEntity
            {
                Id = Guid.NewGuid(),
                PlayerId = buyerId,
                ItemId = item.Id.Value,
                Quantity = quantity,
                UpdatedAt = now
            });
            return;
        }

        var stack = PlayerItemStackEntityMapper.MapToDomain(existingStackEntity);
        if (stack.Quantity + quantity > item.MaxStack)
        {
            throw new MarketPurchaseFailedException(StatusCodes.Status422UnprocessableEntity, "スタック上限を超えるため購入できません。");
        }

        stack.AddQuantity(quantity, item.MaxStack, now);
        PlayerItemStackEntityMapper.ApplyEntity(existingStackEntity, stack);
    }

    private static async Task<int> CalculateUsedSlotsForPlayerAsync(AppDbContext dbContext, Guid playerId, DateTimeOffset now)
    {
        var listedEquipmentIds = await dbContext.MarketListings
            .AsNoTracking()
            .Where(x => x.SellerId == playerId && x.ExpiresAt > now && x.RemainingQuantity > 0 && x.PlayerEquipmentId != null)
            .Select(x => x.PlayerEquipmentId!.Value)
            .ToHashSetAsync();
        var playerEquipments = await dbContext.PlayerEquipments
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId)
            .ToListAsync();
        var stackCount = await dbContext.PlayerItemStacks
            .AsNoTracking()
            .CountAsync(x => x.PlayerId == playerId);

        return playerEquipments.Count(x => x.EquipmentStatus != (int)EquipmentStatus.Equipped && !listedEquipmentIds.Contains(x.Id)) + stackCount;
    }

    private static async Task<MarketListingEntity?> LockMarketListingAsync(AppDbContext dbContext, Guid listingId)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await dbContext.MarketListings.SingleOrDefaultAsync(x => x.Id == listingId);
        }

        return await dbContext.MarketListings
            .FromSqlInterpolated($"SELECT * FROM internal.market_listings WHERE id = {listingId} FOR UPDATE")
            .SingleOrDefaultAsync();
    }

    private static async Task<PlayerEquipmentEntity?> LockPlayerEquipmentAsync(AppDbContext dbContext, Guid playerEquipmentId)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await dbContext.PlayerEquipments.SingleOrDefaultAsync(x => x.Id == playerEquipmentId);
        }

        return await dbContext.PlayerEquipments
            .FromSqlInterpolated($"SELECT * FROM internal.player_equipments WHERE id = {playerEquipmentId} FOR UPDATE")
            .SingleOrDefaultAsync();
    }

    private static async Task<PlayerItemStackEntity?> FindPlayerItemStackAsync(AppDbContext dbContext, Guid playerId, int itemId)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await dbContext.PlayerItemStacks.SingleOrDefaultAsync(x => x.PlayerId == playerId && x.ItemId == itemId);
        }

        return await dbContext.PlayerItemStacks
            .FromSqlInterpolated($"SELECT * FROM internal.player_item_stacks WHERE player_id = {playerId} AND item_id = {itemId} FOR UPDATE")
            .SingleOrDefaultAsync();
    }
}

public sealed record PurchaseMarketListingResult(
    Guid BuyerId,
    Guid SellerId,
    string ItemName,
    int PurchasedQuantity,
    int TotalPrice,
    int BuyerGold);

public sealed class MarketPurchaseFailedException(int statusCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;
}
