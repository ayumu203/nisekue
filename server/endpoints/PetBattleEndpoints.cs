using System.Security.Claims;
using server.application.pet_battle;
using server.domain.battle;
using server.domain.move;
using server.domain.pet;
using server.domain.pet_battle;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.endpoints;

internal static class PetBattleEndpoints
{
    internal static WebApplication MapPetBattleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/pet-battles").RequireAuthorization();

        group.MapPost("/match", async (ClaimsPrincipal user, PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            try
            {
                var room = await petBattleService.MatchAsync(playerId.Value);
                return Results.Ok(MapRoom(room));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        });

        group.MapGet("/status", async (ClaimsPrincipal user, PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            var (room, run) = await petBattleService.GetStatusAsync(playerId.Value);
            return Results.Ok(new
            {
                room = room is null ? null : MapRoom(room),
                run = run is null ? null : MapRun(run)
            });
        });

        group.MapGet("/stats", async (ClaimsPrincipal user, PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            var stats = await petBattleService.GetStatsAsync(playerId.Value);
            return Results.Ok(stats is null ? null : MapStats(stats));
        });

        group.MapGet("/rooms/{roomId:guid}", async (Guid roomId, ClaimsPrincipal user, PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            try
            {
                var room = await petBattleService.GetRoomAsync(new PetBattleRoomId(roomId));
                if (room.OwnerPlayerId != playerId.Value && room.OpponentPlayerId != playerId.Value)
                {
                    return Results.Forbid();
                }

                return Results.Ok(MapRoom(room));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        group.MapPut("/rooms/{roomId:guid}/slots", async (
            Guid roomId,
            AssignPetBattleSlotRequest request,
            ClaimsPrincipal user,
            PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            try
            {
                var room = await petBattleService.AssignSlotAsync(
                    new PetBattleRoomId(roomId),
                    new PlayerPetId(request.PetId),
                    request.Row,
                    request.Column,
                    playerId.Value);
                return Results.Ok(MapRoom(room));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapDelete("/rooms/{roomId:guid}/slots/{petId:guid}", async (
            Guid roomId,
            Guid petId,
            ClaimsPrincipal user,
            PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            try
            {
                var room = await petBattleService.RemoveSlotAsync(
                    new PetBattleRoomId(roomId),
                    new PlayerPetId(petId),
                    playerId.Value);
                return Results.Ok(MapRoom(room));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapPost("/rooms/{roomId:guid}/start", async (
            Guid roomId,
            ClaimsPrincipal user,
            PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            try
            {
                var run = await petBattleService.StartBattleAsync(new PetBattleRoomId(roomId), playerId.Value);
                return Results.Ok(MapRun(run));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapGet("/runs/{runId:guid}", async (Guid runId, ClaimsPrincipal user, PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            try
            {
                var run = await petBattleService.GetRunAsync(new PetBattleRunId(runId));
                if (run.OwnerPlayerId != playerId.Value && run.OpponentPlayerId != playerId.Value)
                {
                    return Results.Forbid();
                }

                return Results.Ok(MapRun(run));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        group.MapPost("/runs/{runId:guid}/commands", async (
            Guid runId,
            SubmitPetBattleCommandRequest request,
            ClaimsPrincipal user,
            PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            if ((request.TargetRow is null) != (request.TargetColumn is null))
            {
                return Results.BadRequest(new { message = "targetRow と targetColumn は両方指定するか、両方省略してください。" });
            }

            try
            {
                var participantId = new PetBattleParticipantId(request.ParticipantId);
                var command = new PetBattleSubmittedCommand(
                    participantId,
                    request.TurnNo,
                    request.ActionKind,
                    DateTimeOffset.UtcNow,
                    request.MoveId is null ? null : new MoveId(request.MoveId.Value),
                    request.TargetRow is null ? null : new BattlePosition(request.TargetRow.Value, request.TargetColumn!.Value));

                var result = await petBattleService.SubmitCommandAsync(
                    new PetBattleRunId(runId), participantId, command, playerId.Value);

                return Results.Ok(new
                {
                    accepted = true,
                    resolvedInThisRequest = result.ResolvedInThisRequest,
                    run = MapRun(result.Run)
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapPost("/runs/{runId:guid}/abort", async (
            Guid runId,
            ClaimsPrincipal user,
            PetBattleService petBattleService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            try
            {
                var run = await petBattleService.AbortAsync(new PetBattleRunId(runId), playerId.Value);
                return Results.Ok(MapRun(run));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        return app;
    }

    private static object MapRoom(PetBattleRoom room) => new
    {
        roomId = room.Id.Value,
        status = room.Status.ToString(),
        ownerPlayerId = room.OwnerPlayerId.Value,
        opponentPlayerId = room.OpponentPlayerId.Value,
        slots = room.Slots.Select(s => new
        {
            petId = s.PetId.Value,
            row = s.Row.ToString(),
            column = s.Column.ToString()
        }).ToArray(),
        createdAt = room.CreatedAt
    };

    private static object MapRun(PetBattleRun run) => new
    {
        runId = run.Id.Value,
        roomId = run.RoomId.Value,
        status = run.Status.ToString(),
        ownerPlayerId = run.OwnerPlayerId.Value,
        opponentPlayerId = run.OpponentPlayerId.Value,
        winnerPlayerId = run.WinnerPlayerId?.Value,
        currentTurnNo = run.TurnState.CurrentTurnNo,
        actionDeadlineAt = run.TurnState.ActionDeadlineAt,
        startedAt = run.StartedAt,
        endedAt = run.EndedAt,
        submittedParticipantIds = run.TurnState.PendingCommands.Select(c => c.ParticipantId.Value).ToArray(),
        ownerMembers = run.OwnerSnapshots.Select(s => MapMember(s, run.OwnerMemberStates)).ToArray(),
        opponentMembers = run.OpponentSnapshots.Select(s => MapMember(s, run.OpponentMemberStates)).ToArray(),
        lastTurnResults = run.LastTurnResults is null ? null : MapLastTurnResults(run.LastTurnResults)
    };

    private static object MapMember(
        PetBattlePartyMemberSnapshot snapshot,
        IReadOnlyList<PetBattlePartyMemberState> memberStates)
    {
        var state = memberStates.FirstOrDefault(m => m.ParticipantId == snapshot.ParticipantId);
        return new
        {
            participantId = snapshot.ParticipantId.Value,
            enemyDefinitionId = snapshot.EnemyDefinitionId,
            displayName = snapshot.DisplayName,
            imagePath = snapshot.ImagePath,
            maxHp = snapshot.BaseStatus.MaxHp,
            maxMp = snapshot.BaseStatus.MaxMp,
            currentHp = state?.CurrentHp ?? snapshot.BaseStatus.MaxHp,
            currentMp = state?.CurrentMp ?? snapshot.BaseStatus.MaxMp,
            isDead = state?.IsDead ?? false,
            moveIds = snapshot.MoveIds,
            startRow = snapshot.StartRow.ToString(),
            startColumn = snapshot.StartColumn.ToString()
        };
    }

    private static object MapLastTurnResults(PetBattleLastTurnResults results) => new
    {
        turnNo = results.TurnNo,
        resolvedAt = results.ResolvedAt,
        actions = results.Actions.Select(a => new
        {
            actorParticipantId = a.ActorParticipantId,
            actorDisplayName = a.ActorDisplayName,
            actionKind = a.ActionKind,
            moveId = a.MoveId,
            moveName = a.MoveName,
            succeeded = a.Succeeded,
            targetSummaries = a.TargetSummaries.Select(t => new
            {
                targetParticipantId = t.TargetParticipantId,
                targetDisplayName = t.TargetDisplayName,
                resultType = t.ResultType,
                hpChange = t.HpChange,
                mpChange = t.MpChange,
                appliedEffects = t.AppliedEffects,
                isDeadAfterAction = t.IsDeadAfterAction
            }).ToArray()
        }).ToArray()
    };

    private static object MapStats(PlayerPetBattleStats stats) => new
    {
        rating = stats.Rating,
        wins = stats.Wins,
        losses = stats.Losses,
        totalBattles = stats.TotalBattles,
        updatedAt = stats.UpdatedAt
    };
}
