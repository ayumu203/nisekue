using System.Security.Claims;
using server.application.debug;

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
        _ = group;
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
