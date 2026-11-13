using server.domain.player;

namespace server.application.debug;

/// <summary>
/// デバッグ機能で、アイテム・装備を入手させる。
/// 通常の入手と違い、所持枠の上限は確認しない。
/// </summary>
public class DebugInventoryService(
    IItemRepository itemRepository,
    IEquipmentRepository equipmentRepository,
    IPlayerItemStackRepository playerItemStackRepository,
    IPlayerEquipmentRepository playerEquipmentRepository)
{
    public const int MaxEquipmentCountPerRequest = 20;

    /// <summary>アイテムを quantity 個入手させる。所持数はスタック上限で頭打ちになる。入手後の所持数を返す。</summary>
    public async Task<int> GrantItemAsync(PlayerId playerId, int itemId, int quantity, DateTimeOffset now)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "個数は1以上である必要があります。");
        }

        var item = await itemRepository.GetAsync(new ItemId(itemId))
            ?? throw new ArgumentException($"アイテム {itemId} は存在しません。", nameof(itemId));

        var stacks = (await playerItemStackRepository.GetByPlayerAsync(playerId)).ToList();
        var stack = stacks.FirstOrDefault(x => x.ItemId == item.Id);
        if (stack is null)
        {
            stack = new PlayerItemStack(
                PlayerItemStackId.New(),
                playerId,
                item.Id,
                Math.Min(quantity, item.MaxStack),
                now);
            stacks.Add(stack);
        }
        else
        {
            stack.AddQuantity(quantity, item.MaxStack, now);
        }

        await playerItemStackRepository.SaveAsync(stacks);
        return stack.Quantity;
    }

    /// <summary>装備を count 個入手させる（所持品に入る。耐久は最大、熟練度 0）。</summary>
    public async Task<int> GrantEquipmentAsync(PlayerId playerId, int equipmentId, int count, int plusValue, DateTimeOffset now)
    {
        if (count is <= 0 or > MaxEquipmentCountPerRequest)
        {
            throw new ArgumentOutOfRangeException(nameof(count), $"個数は1以上{MaxEquipmentCountPerRequest}以下である必要があります。");
        }

        if (plusValue is < 0 or > PlayerEquipment.MaxPlusValue)
        {
            throw new ArgumentOutOfRangeException(nameof(plusValue), $"強化値は0以上{PlayerEquipment.MaxPlusValue}以下である必要があります。");
        }

        var equipment = await equipmentRepository.GetAsync(new EquipmentId(equipmentId))
            ?? throw new ArgumentException($"装備 {equipmentId} は存在しません。", nameof(equipmentId));

        var playerEquipments = (await playerEquipmentRepository.GetByPlayerAsync(playerId)).ToList();
        for (var i = 0; i < count; i++)
        {
            playerEquipments.Add(new PlayerEquipment(
                PlayerEquipmentId.New(),
                playerId,
                equipment.Id,
                equipment.Type,
                EquipmentStatus.Inventory,
                equipment.MaxDurability,
                mastery: 0,
                plusValue,
                now,
                now));
        }

        await playerEquipmentRepository.SaveAsync(playerEquipments);
        return count;
    }
}
