using System.Security.Claims;
using server.application.debug;
using server.application.player;
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
        group.MapGet("/templates", GetTemplates);
        group.MapPost("/player/template", ApplyTemplate);
        group.MapPost("/player/level", LevelUp);
        group.MapPost("/player/master-jobs", MasterJobs);
        group.MapPost("/player/job", ChangeJob);
        group.MapGet("/items", GetItems);
        group.MapGet("/equipments", GetEquipments);
        group.MapPost("/player/items", GrantItem);
        group.MapPost("/player/equipments", GrantEquipment);
        group.MapPost("/player/gold", GrantGold);
        group.MapPost("/player/rebirth", Rebirth);
        group.MapPost("/player/cooldowns/reset", ResetCooldowns);
        group.MapPost("/treasure-map/complete", CompleteTreasureMap);
    }

    private static async Task<IResult> GrantGold(
        ClaimsPrincipal user,
        DebugGrantGoldRequest request,
        DebugPlayerService debugPlayerService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var gold = await debugPlayerService.GrantGoldAsync(playerId, request.Amount);
        return Results.Ok(new { gold });
    }

    private static async Task<IResult> Rebirth(ClaimsPrincipal user, PlayerRebirthService playerRebirthService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var player = await playerRebirthService.RebirthAsync(playerId, ignoreRequirements: true);
        return Results.Ok(new { rebirthCount = player.RebirthCount, level = player.Level });
    }

    private static async Task<IResult> ResetCooldowns(ClaimsPrincipal user, DebugWaitService debugWaitService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        await debugWaitService.ResetCooldownsAsync(playerId);
        return Results.NoContent();
    }

    private static async Task<IResult> CompleteTreasureMap(ClaimsPrincipal user, DebugWaitService debugWaitService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var endsAt = await debugWaitService.CompleteTreasureMapAsync(playerId, DateTimeOffset.UtcNow);
        return Results.Ok(new { endsAt });
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

    private static IResult GetTemplates(DebugTemplateService debugTemplateService)
    {
        return Results.Ok(debugTemplateService.GetTemplates().Select(x => new
        {
            templateId = x.Id,
            name = x.Job is { } job
                ? $"{EndpointHelpers.GetJobDisplayName(job)}マスター"
                : "作成直後（見習い Lv1）"
        }));
    }

    private static async Task<IResult> ApplyTemplate(
        ClaimsPrincipal user,
        DebugApplyTemplateRequest request,
        DebugTemplateService debugTemplateService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var result = await debugTemplateService.ApplyAsync(playerId, request.TemplateId ?? string.Empty);
        return Results.Ok(new
        {
            level = result.Level,
            job = result.Job.ToString(),
            masteredJobs = result.MasteredJobs.Select(x => x.ToString())
        });
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

    private static async Task<IResult> ChangeJob(
        ClaimsPrincipal user,
        DebugChangeJobRequest request,
        DebugPlayerService debugPlayerService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var player = await debugPlayerService.ChangeJobAsync(playerId, request.Job);
        return Results.Ok(new { job = player.Job.ToString(), jobLevel = player.JobLevel });
    }

    private static async Task<IResult> MasterJobs(
        ClaimsPrincipal user,
        DebugMasterJobsRequest request,
        DebugPlayerService debugPlayerService)
    {
        var playerId = EndpointHelpers.TryGetPlayerId(user)!.Value;
        var result = await debugPlayerService.MasterJobsAsync(playerId, request.Jobs ?? [], request.UnlockRoadmap);
        return Results.Ok(new
        {
            newlyMasteredJobs = result.NewlyMasteredJobs.Select(x => x.ToString()),
            newlyLearnedMoveIds = result.NewlyLearnedMoveIds
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

internal sealed record DebugApplyTemplateRequest(string? TemplateId);

internal sealed record DebugChangeJobRequest(Job Job);

internal sealed record DebugMasterJobsRequest(IReadOnlyList<Job>? Jobs, bool UnlockRoadmap);

internal sealed record DebugGrantItemRequest(int ItemId, int Quantity);

internal sealed record DebugGrantEquipmentRequest(int EquipmentId, int Count, int? PlusValue);

internal sealed record DebugGrantGoldRequest(int Amount);
