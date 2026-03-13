using System.Security.Claims;
using server.application.training;
using server.domain.training;

namespace server.endpoints;

internal static class TrainingEndpoints
{
    internal static WebApplication MapTrainingEndpoints(this WebApplication app)
    {
        app.MapGet("/training/enemies", async (TrainingService trainingService) =>
        {
            var enemies = await trainingService.GetTrainingEnemies();
            return Results.Ok(enemies);
        }).RequireAuthorization();

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

        return app;
    }
}
