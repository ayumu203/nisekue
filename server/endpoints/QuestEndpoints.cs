using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using server.application.quest;
using server.domain.battle;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.endpoints;

internal static class QuestEndpoints
{
    internal static WebApplication MapQuestEndpoints(this WebApplication app)
    {
        var questGroup = app.MapGroup("/quest").RequireAuthorization();

        questGroup.MapGet("/stages", async (ClaimsPrincipal user, QuestRoomService questRoomService, QuestResponseMapper responseMapper) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            var stages = await questRoomService.GetVisibleStagesAsync(playerId);
            var payload = await responseMapper.MapQuestStageSummariesAsync(stages);
            return Results.Ok(payload);
        });

        questGroup.MapPost("/rooms", async (ClaimsPrincipal user, CreateQuestRoomRequest request, QuestRoomService questRoomService, QuestResponseMapper responseMapper) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var room = await questRoomService.CreateRoomAsync(
                    playerId.Value,
                    new QuestStageId(request.StageId),
                    request.Mode,
                    request.MinRequiredLevel,
                    request.AllowedPlayerIds?.Select(id => new PlayerId(id)).ToArray());
                var viewer = await questRoomService.GetViewerAsync(playerId.Value);
                return Results.Ok(await responseMapper.MapQuestRoomDetailAsync(room, viewer));
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
        });

        questGroup.MapGet("/rooms", async (
            ClaimsPrincipal user,
            int? stageId,
            QuestRoomMode? mode,
            QuestRoomStatus? status,
            Guid? ownerPlayerId,
            int? page,
            int? pageSize,
            IQuestRoomRepository questRoomRepository,
            QuestRoomService questRoomService,
            QuestResponseMapper responseMapper) =>
        {
            var viewerPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (viewerPlayerId is null)
            {
                return Results.Unauthorized();
            }

            Player viewer;
            try
            {
                viewer = await questRoomService.GetViewerAsync(viewerPlayerId.Value);
            }
            catch (KeyNotFoundException)
            {
                return Results.Unauthorized();
            }

            var visibleStageIds = await questRoomService.GetVisibleStageIdsAsync(viewer);
            var rooms = await questRoomRepository.SearchAsync(new QuestRoomSearchCondition(
                stageId is null ? null : new QuestStageId(stageId.Value),
                mode,
                status ?? QuestRoomStatus.Recruiting,
                ownerPlayerId is null ? null : new PlayerId(ownerPlayerId.Value),
                viewerPlayerId.Value,
                page ?? 1,
                pageSize ?? 20,
                visibleStageIds: visibleStageIds));
            var viewerHasActiveRun = await questRoomService.ViewerHasActiveRunAsync(viewerPlayerId.Value);

            var payload = new List<object>(rooms.Count);
            foreach (var room in rooms)
            {
                payload.Add(await responseMapper.MapQuestRoomSummaryAsync(room, viewer, viewerHasActiveRun));
            }

            return Results.Ok(payload);
        });

        questGroup.MapGet("/rooms/{roomId:guid}", async (
            Guid roomId,
            ClaimsPrincipal user,
            IQuestRoomRepository questRoomRepository,
            QuestRoomService questRoomService,
            QuestResponseMapper responseMapper) =>
        {
            var viewerPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (viewerPlayerId is null)
            {
                return Results.Unauthorized();
            }

            var room = await questRoomRepository.GetAsync(new QuestRoomId(roomId));
            return room is null
                ? Results.NotFound(new { message = "ルームが見つかりません。" })
                : Results.Ok(await responseMapper.MapQuestRoomDetailAsync(room, await questRoomService.GetViewerAsync(viewerPlayerId.Value)));
        });

        questGroup.MapGet("/rooms/{roomId:guid}/run", async (
            Guid roomId,
            IQuestRunRepository questRunRepository,
            QuestResponseMapper responseMapper) =>
        {
            var run = await questRunRepository.GetByRoomIdAsync(new QuestRoomId(roomId));
            return run is null
                ? Results.NotFound(new { message = "進行中クエストが見つかりません。" })
                : Results.Ok(await responseMapper.MapQuestRunDetailAsync(run));
        });

        questGroup.MapPost("/rooms/{roomId:guid}/join", async (Guid roomId, ClaimsPrincipal user, QuestRoomService questRoomService, QuestResponseMapper responseMapper) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var room = await questRoomService.JoinRoomAsync(new QuestRoomId(roomId), playerId.Value);
                return Results.Ok(await responseMapper.MapQuestRoomDetailAsync(room, await questRoomService.GetViewerAsync(playerId.Value)));
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
        });

        questGroup.MapPut("/rooms/{roomId:guid}/positions", async (
            Guid roomId,
            ClaimsPrincipal user,
            UpdateQuestRoomPositionRequest request,
            IQuestRoomRepository questRoomRepository,
            QuestRoomService questRoomService,
            QuestResponseMapper responseMapper) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var room = await questRoomRepository.GetAsync(new QuestRoomId(roomId));
            if (room is null)
            {
                return Results.NotFound(new { message = "ルームが見つかりません。" });
            }

            var targetParticipant = room.Participants.FirstOrDefault(x => x.Id == new QuestParticipantId(request.ParticipantId));
            if (targetParticipant is null)
            {
                return Results.NotFound(new { message = "参加者が見つかりません。" });
            }

            var canMove = room.OwnerId == playerId.Value || targetParticipant.PlayerId == playerId.Value;
            if (!canMove)
            {
                return Results.Forbid();
            }

            try
            {
                room.AssignPosition(
                    targetParticipant.Id,
                    new BattlePosition(request.Row, request.Column));
                await questRoomRepository.SaveAsync(room);
                return Results.Ok(await responseMapper.MapQuestRoomDetailAsync(room, await questRoomService.GetViewerAsync(playerId.Value)));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        });

        questGroup.MapPost("/rooms/{roomId:guid}/start", async (
            Guid roomId,
            ClaimsPrincipal user,
            IQuestRoomRepository questRoomRepository,
            QuestRoomService questRoomService,
            QuestResponseMapper responseMapper) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var room = await questRoomRepository.GetAsync(new QuestRoomId(roomId));
            if (room is null)
            {
                return Results.NotFound(new { message = "ルームが見つかりません。" });
            }

            if (room.OwnerId != playerId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                var run = await questRoomService.StartAsync(room.Id);
                return Results.Ok(await responseMapper.MapQuestRunDetailAsync(run));
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
        });

        questGroup.MapPost("/rooms/{roomId:guid}/cancel", async (
            Guid roomId,
            ClaimsPrincipal user,
            QuestRoomService questRoomService,
            QuestResponseMapper responseMapper) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var room = await questRoomService.CancelRoomAsync(new QuestRoomId(roomId), playerId.Value);
                return Results.Ok(await responseMapper.MapQuestRoomDetailAsync(room, await questRoomService.GetViewerAsync(playerId.Value)));
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
        });

        questGroup.MapPut("/rooms/{roomId:guid}/restrictions", async (
            Guid roomId,
            ClaimsPrincipal user,
            UpdateQuestRoomRestrictionsRequest request,
            IQuestRoomRepository questRoomRepository,
            QuestRoomService questRoomService,
            QuestResponseMapper responseMapper) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var room = await questRoomRepository.GetAsync(new QuestRoomId(roomId));
            if (room is null)
            {
                return Results.NotFound(new { message = "ルームが見つかりません。" });
            }

            if (room.OwnerId != playerId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                room = await questRoomService.UpdateRestrictionsAsync(
                    new QuestRoomId(roomId),
                    playerId.Value,
                    request.MinRequiredLevel,
                    request.AllowedPlayerIds?.Select(id => new PlayerId(id)).ToArray());
                return Results.Ok(await responseMapper.MapQuestRoomDetailAsync(room, await questRoomService.GetViewerAsync(playerId.Value)));
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
        });

        questGroup.MapGet("/runs/{runId:guid}", async (
            Guid runId,
            QuestRunService questRunService,
            QuestResponseMapper responseMapper) =>
        {
            try
            {
                var run = await questRunService.GetDetailAsync(new QuestRunId(runId));
                return Results.Ok(await responseMapper.MapQuestRunDetailAsync(run));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        questGroup.MapGet("/runs/active", async (
            ClaimsPrincipal user,
            IQuestRunRepository questRunRepository,
            QuestResponseMapper responseMapper) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var run = await questRunRepository.GetActiveByPlayerAsync(playerId.Value);
            return run is null
                ? Results.NotFound(new { message = "進行中クエストが見つかりません。" })
                : Results.Ok(await responseMapper.MapQuestRunDetailAsync(run));
        });

        questGroup.MapPost("/runs/{runId:guid}/commands", async (
            Guid runId,
            ClaimsPrincipal user,
            SubmitQuestCommandRequest request,
            IQuestRunRepository questRunRepository,
            IQuestRoomRepository questRoomRepository,
            QuestRunService questRunService,
            QuestResponseMapper responseMapper,
            IHubContext<QuestRunHub> hubContext) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            // 参加者検証はルームIDだけで足りるため、集約全体（last_turn_results_json 含む）を読まない。
            var roomId = await questRunRepository.GetRoomIdAsync(new QuestRunId(runId));
            if (roomId is null)
            {
                return Results.NotFound(new { message = "クエスト進行情報が見つかりません。" });
            }

            var room = await questRoomRepository.GetAsync(roomId.Value);
            if (room is null)
            {
                return Results.NotFound(new { message = "ルームが見つかりません。" });
            }

            var participantId = new QuestParticipantId(request.ParticipantId);
            var participant = room.Participants.FirstOrDefault(x => x.Id == participantId);
            if (participant is null)
            {
                return Results.NotFound(new { message = "参加者が見つかりません。" });
            }

            if (participant.PlayerId != playerId.Value)
            {
                return Results.Forbid();
            }

            if ((request.TargetRow is null) != (request.TargetColumn is null))
            {
                return Results.BadRequest(new { message = "targetRow と targetColumn は両方指定するか、両方省略してください。" });
            }

            try
            {
                var command = new QuestSubmittedCommand(
                    participantId,
                    request.TurnNo,
                    request.ActionKind,
                    DateTimeOffset.UtcNow,
                    request.MoveId is null ? null : new MoveId(request.MoveId.Value),
                    request.TargetRow is null ? null : new BattlePosition(request.TargetRow.Value, request.TargetColumn!.Value));

                var result = await questRunService.SubmitCommandAsync(new QuestRunId(runId), participantId, command);
                var payload = await responseMapper.MapQuestRunDetailAsync(result.Run);
                await hubContext.Clients.Group(result.Run.Id.Value.ToString()).SendAsync("QuestRunUpdated", payload);
                return Results.Ok(new
                {
                    accepted = true,
                    resolvedInThisRequest = result.ResolvedInThisRequest
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
        });

        questGroup.MapPost("/runs/{runId:guid}/escape", async (
            Guid runId,
            ClaimsPrincipal user,
            QuestRunService questRunService,
            QuestResponseMapper responseMapper,
            IHubContext<QuestRunHub> hubContext) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var run = await questRunService.EscapeAsync(new QuestRunId(runId), playerId.Value);
                var payload = await responseMapper.MapQuestRunDetailAsync(run);
                await hubContext.Clients.Group(run.Id.Value.ToString()).SendAsync("QuestRunUpdated", payload);
                return Results.Ok(payload);
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
        });

        questGroup.MapPost("/runs/{runId:guid}/manual-control/request", async (
            Guid runId,
            ClaimsPrincipal user,
            ManualControlRequest request,
            IQuestRunRepository questRunRepository,
            IQuestRoomRepository questRoomRepository,
            QuestRunService questRunService,
            QuestResponseMapper responseMapper,
            IHubContext<QuestRunHub> hubContext) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var run = await questRunRepository.GetAsync(new QuestRunId(runId));
            if (run is null)
            {
                return Results.NotFound(new { message = "クエスト進行情報が見つかりません。" });
            }

            var room = await questRoomRepository.GetAsync(run.RoomId);
            if (room is null)
            {
                return Results.NotFound(new { message = "ルームが見つかりません。" });
            }

            var participantId = new QuestParticipantId(request.ParticipantId);
            var participant = room.Participants.FirstOrDefault(x => x.Id == participantId);
            if (participant is null)
            {
                return Results.NotFound(new { message = "参加者が見つかりません。" });
            }

            if (participant.PlayerId != playerId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                var updatedRun = await questRunService.RequestManualControlAsync(run.Id, participantId);
                var payload = await responseMapper.MapQuestRunDetailAsync(updatedRun);
                await hubContext.Clients.Group(updatedRun.Id.Value.ToString()).SendAsync("QuestRunUpdated", payload);
                return Results.Ok(new { accepted = true });
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

        questGroup.MapPost("/runs/{runId:guid}/manual-control/approve", async (
            Guid runId,
            ClaimsPrincipal user,
            ManualControlApproveRequest request,
            IQuestRunRepository questRunRepository,
            IQuestRoomRepository questRoomRepository,
            QuestRunService questRunService,
            QuestResponseMapper responseMapper,
            IHubContext<QuestRunHub> hubContext) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var run = await questRunRepository.GetAsync(new QuestRunId(runId));
            if (run is null)
            {
                return Results.NotFound(new { message = "クエスト進行情報が見つかりません。" });
            }

            var room = await questRoomRepository.GetAsync(run.RoomId);
            if (room is null)
            {
                return Results.NotFound(new { message = "ルームが見つかりません。" });
            }

            if (room.OwnerId != playerId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                var updatedRun = await questRunService.ApproveManualControlAsync(
                    run.Id,
                    new QuestParticipantId(request.ParticipantId),
                    playerId.Value);
                var payload = await responseMapper.MapQuestRunDetailAsync(updatedRun);
                await hubContext.Clients.Group(updatedRun.Id.Value.ToString()).SendAsync("QuestRunUpdated", payload);
                return Results.Ok(new { accepted = true });
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

        questGroup.MapPost("/runs/{runId:guid}/chat", async (
            Guid runId,
            ClaimsPrincipal user,
            PostQuestChatMessageRequest request,
            IQuestRunRepository questRunRepository,
            IQuestRoomRepository questRoomRepository,
            QuestRunService questRunService,
            QuestResponseMapper responseMapper,
            IHubContext<QuestRunHub> hubContext) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var run = await questRunRepository.GetAsync(new QuestRunId(runId));
            if (run is null)
            {
                return Results.NotFound(new { message = "クエスト進行情報が見つかりません。" });
            }

            var room = await questRoomRepository.GetAsync(run.RoomId);
            if (room is null)
            {
                return Results.NotFound(new { message = "ルームが見つかりません。" });
            }

            var participantId = new QuestParticipantId(request.ParticipantId);
            var participant = room.Participants.FirstOrDefault(x => x.Id == participantId);
            if (participant is null)
            {
                return Results.NotFound(new { message = "参加者が見つかりません。" });
            }

            if (participant.PlayerId != playerId.Value)
            {
                return Results.Forbid();
            }

            var snapshot = run.PartySnapshots.FirstOrDefault(x => x.ParticipantId == participantId);
            if (snapshot is null)
            {
                return Results.NotFound(new { message = "進行中クエストの参加者スナップショットが見つかりません。" });
            }

            try
            {
                var message = new QuestChatMessage(
                    run.TurnState.CurrentTurnNo,
                    participantId,
                    snapshot.DisplayName,
                    snapshot.ImagePath,
                    request.Message,
                    DateTimeOffset.UtcNow);
                var updatedRun = await questRunService.AddChatMessageAsync(run.Id, message);
                var payload = await responseMapper.MapQuestRunDetailAsync(updatedRun);
                await hubContext.Clients.Group(updatedRun.Id.Value.ToString()).SendAsync("QuestRunUpdated", payload);
                return Results.Ok(new { accepted = true });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        });

        app.MapHub<QuestRunHub>("/quest-hubs/runs").RequireAuthorization();
        return app;
    }
}
