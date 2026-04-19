using System.Security.Claims;
using server.application.training;
using server.domain.player;
using server.domain.training;

namespace server.endpoints;

internal static class TrainingEndpoints
{
    internal static WebApplication MapTrainingEndpoints(this WebApplication app)
    {
        app.MapGet("/training/enemies", async (ClaimsPrincipal user, TrainingService trainingService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var enemies = await trainingService.GetTrainingEnemies(playerId.Value);
                return Results.Ok(enemies);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        }).RequireAuthorization()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/training/execute", async (ClaimsPrincipal user, ExecuteTrainingRequest request, TrainingService trainingService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var result = await trainingService.ExecuteTraining(
                    playerId.Value,
                    new TrainingEnemyId(request.EnemyId),
                    request.MoveIds);
                return Results.Ok(result);
            }
            catch (TrainingCooldownException ex)
            {
                var retryAfterSeconds = Math.Max(
                    1,
                    (int)Math.Ceiling((ex.CooldownUntil - DateTimeOffset.UtcNow).TotalSeconds));
                return Results.Json(
                    new { message = ex.Message, retryAfterSeconds, cooldownUntil = ex.CooldownUntil },
                    statusCode: StatusCodes.Status429TooManyRequests);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization();

        app.MapPost("/training/execute-pvp", async (ClaimsPrincipal user, ExecutePvpTrainingRequest request, TrainingService trainingService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            if (request.OpponentPlayerId == playerId.Value.Value)
            {
                return Results.BadRequest(new { message = "自分自身とは対戦できません。" });
            }

            try
            {
                var result = await trainingService.ExecutePvpTraining(
                    playerId.Value,
                    new PlayerId(request.OpponentPlayerId),
                    request.MoveIds);
                return Results.Ok(result);
            }
            catch (TrainingCooldownException ex)
            {
                var retryAfterSeconds = Math.Max(
                    1,
                    (int)Math.Ceiling((ex.CooldownUntil - DateTimeOffset.UtcNow).TotalSeconds));
                return Results.Json(
                    new { message = ex.Message, retryAfterSeconds, cooldownUntil = ex.CooldownUntil },
                    statusCode: StatusCodes.Status429TooManyRequests);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization();

        return app;
    }
}
