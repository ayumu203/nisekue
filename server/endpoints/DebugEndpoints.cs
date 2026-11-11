using System.Security.Claims;
using server.application.debug;
using server.domain.move;
using server.domain.player;

namespace server.endpoints;

internal static class DebugEndpoints
{
    internal static WebApplication MapDebugEndpoints(this WebApplication app)
    {
        app.MapGet("/debug/status", GetStatus).RequireAuthorization();

        var settings = app.Services.GetRequiredService<DebugSettings>();
        if (!settings.Enabled)
        {
            return app;
        }

        // デバッグ用の操作は、このグループに追加する（認証と DebugAccessService の判定はグループで共通）
        var group = app.MapGroup("/debug")
            .RequireAuthorization()
            .AddEndpointFilter(RequireDebugAccess)
            .ExcludeFromDescription();
        MapDebugOperations(group);

        return app;
    }

    /// <summary>デバッグ用の操作を登録する。</summary>
    private static void MapDebugOperations(RouteGroupBuilder group)
    {
        group.MapGet("/moves", GetMoves);
        group.MapPut("/player/status", UpdateStatus);
        group.MapPost("/player/moves", LearnMove);
        group.MapDelete("/player/moves/{slotIndex:int}", ClearMoveSlot);
        group.MapPost("/player/level", LevelUp);
        group.MapGet("/items", GetItems);
        group.MapGet("/equipments", GetEquipments);
        group.MapPost("/player/items", GrantItem);
        group.MapPost("/player/equipments", GrantEquipment);
    }

    private static async Task<IResult> GetItems(IItemRepository itemRepository)
    {
        var items = await itemRepository.GetAllAsync();
        return Results.Ok(items
            .OrderBy(x => x.Id.Value)
            .Select(x => new { itemId = x.Id.Value, name = x.Name, maxStack = x.MaxStack }));
    }

    private static async Task<IResult> GetEquipments(IEquipmentRepository equipmentRepository)
    {
        var equipments = await equipmentRepository.GetAllAsync();
        return Results.Ok(equipments
            .OrderBy(x => x.Id.Value)
            .Select(x => new { equipmentId = x.Id.Value, name = x.Name, equipmentType = x.Type.ToString() }));
    }

    private static async Task<IResult> GrantItem(
        ClaimsPrincipal user,
        DebugGrantItemRequest request,
        DebugInventoryService debugInventoryService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var quantity = await debugInventoryService.GrantItemAsync(playerId, request.ItemId, request.Quantity, DateTimeOffset.UtcNow);
        return Results.Ok(new { quantity });
    }

    private static async Task<IResult> GrantEquipment(
        ClaimsPrincipal user,
        DebugGrantEquipmentRequest request,
        DebugInventoryService debugInventoryService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var count = await debugInventoryService.GrantEquipmentAsync(
            playerId,
            request.EquipmentId,
            request.Count,
            request.PlusValue ?? 0,
            DateTimeOffset.UtcNow);
        return Results.Ok(new { count });
    }

    private static async Task<IResult> GetMoves(IMoveRepository moveRepository)
    {
        var moves = await moveRepository.GetAllMovesAsync();
        return Results.Ok(moves
            .OrderBy(x => x.Id.Id)
            .Select(x => new { moveId = x.Id.Id, name = x.Name }));
    }

    private static async Task<IResult> UpdateStatus(
        ClaimsPrincipal user,
        DebugUpdateStatusRequest request,
        DebugPlayerService debugPlayerService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        await debugPlayerService.UpdateStatusAsync(playerId, new DebugStatusValues(
            request.MaxHp,
            request.MaxMp,
            request.Strength,
            request.Defense,
            request.Intelligence,
            request.Luck,
            request.Speed));
        return Results.NoContent();
    }

    private static async Task<IResult> LearnMove(
        ClaimsPrincipal user,
        DebugLearnMoveRequest request,
        DebugPlayerService debugPlayerService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        await debugPlayerService.LearnMoveAsync(playerId, request.MoveId, request.SlotIndex);
        return Results.NoContent();
    }

    private static async Task<IResult> LevelUp(
        ClaimsPrincipal user,
        DebugLevelUpRequest request,
        DebugPlayerService debugPlayerService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var result = await debugPlayerService.LevelUpToAsync(playerId, request.Level);
        return Results.Ok(new
        {
            level = result.Level,
            newlyLearnedMoveIds = result.NewlyLearnedMoveIds,
            hasMasteredCurrentJob = result.HasMasteredCurrentJob
        });
    }

    private static async Task<IResult> ClearMoveSlot(
        ClaimsPrincipal user,
        int slotIndex,
        DebugPlayerService debugPlayerService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        await debugPlayerService.ClearMoveSlotAsync(playerId, slotIndex);
        return Results.NoContent();
    }

    private static async Task<IResult> GetStatus(ClaimsPrincipal user, DebugAccessService debugAccessService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user);
        if (playerId is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new { canUseDebug = await debugAccessService.CanUseAsync(playerId.Value) });
    }

    private static async ValueTask<object?> RequireDebugAccess(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(context.HttpContext.User);
        if (playerId is null)
        {
            return Results.Unauthorized();
        }

        var debugAccessService = context.HttpContext.RequestServices.GetRequiredService<DebugAccessService>();
        if (!await debugAccessService.CanUseAsync(playerId.Value))
        {
            return Results.Json(
                new { message = "デバッグ機能を利用する権限がありません。" },
                options: null,
                contentType: null,
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}

internal sealed record DebugUpdateStatusRequest(
    int MaxHp,
    int MaxMp,
    int Strength,
    int Defense,
    int Intelligence,
    int Luck,
    int Speed);

internal sealed record DebugLearnMoveRequest(int MoveId, int? SlotIndex);

internal sealed record DebugLevelUpRequest(int Level);

internal sealed record DebugGrantItemRequest(int ItemId, int Quantity);

internal sealed record DebugGrantEquipmentRequest(int EquipmentId, int Count, int? PlusValue);
