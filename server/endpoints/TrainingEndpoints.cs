using System.Security.Claims;
using server.application.training;
using server.domain.player;
using server.domain.training;
using server.shared.pagination;
using server.shared.constants.training;

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

        app.MapGet("/training/pvp-opponents", async (
            ClaimsPrincipal user,
            int? page,
            int? pageSize,
            int? limit,
            IPlayerRepository playerRepository,
            IJobProfileRepository jobProfileRepository,
            CombatIndexCalculator combatIndexCalculator,
            CombatIndexRankEvaluator combatIndexRankEvaluator) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            if (!PaginationQueryResolver.TryResolve(page, pageSize, limit, out var offset, out var effectiveLimit, out var errorMessage))
            {
                return Results.BadRequest(new { message = errorMessage });
            }

            var player = await playerRepository.GetPlayerAsync(playerId.Value);
            if (player is null)
            {
                return Results.NotFound(new { message = "プレイヤーが見つかりません。" });
            }

            var maxOpponentLevel = (int)(player.Level * TrainingConstants.Battle.PvpOpponentLevelCapMultiplier);
            var pageResult = await playerRepository.GetPvpOpponentsPageAsync(playerId.Value, maxOpponentLevel, offset, effectiveLimit);
            var items = pageResult.Opponents.Select(p => new
            {
                userId = p.Id.Value,
                userName = p.Name,
                imagePath = p.ImagePath,
                level = p.Level,
                job = new
                {
                    code = p.Job.ToString(),
                    value = (int)p.Job,
                    displayName = EndpointHelpers.GetJobDisplayName(p.Job),
                    description = jobProfileRepository.GetByJob(p.Job).Description
                },
                combatIndexRank = combatIndexRankEvaluator.Evaluate(combatIndexCalculator.Calculate(p.Status)).ToString()
            })
                .ToArray();

            return Results.Ok(PagedResponseFactory.Create(
                items,
                pageResult.TotalCount,
                page,
                pageSize,
                offset,
                effectiveLimit));
        }).RequireAuthorization();

        return app;
    }
}
