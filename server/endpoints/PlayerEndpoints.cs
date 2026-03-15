using System.Security.Claims;
using server.application.chat;
using server.domain.move;
using server.domain.player;
using server.shared.constants.player;

namespace server.endpoints;

internal static class PlayerEndpoints
{
    internal static WebApplication MapPlayerEndpoints(this WebApplication app)
    {
        app.MapGet("/players", async (IPlayerRepository playerRepository) =>
        {
            var players = await playerRepository.GetAllAsync();

            return Results.Ok(players.Select(player => new
            {
                userId = player.Id.Value,
                userName = player.Name,
                imagePath = player.ImagePath
            }));
        }).RequireAuthorization();

        app.MapGet("/player", async (ClaimsPrincipal user, IPlayerRepository playerRepository, IMoveRepository moveRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var player = await playerRepository.GetPlayerAsync(playerId.Value);
            if (player is null)
            {
                return Results.NotFound(new
                {
                    message = "プレイヤーが見つかりません。",
                    userId = playerId.Value.Value
                });
            }

            var allMoves = await moveRepository.GetAllMovesAsync();
            return Results.Ok(ToPlayerResponse(player, allMoves));
        }).RequireAuthorization();

        app.MapGet("/players/{playerId:guid}", async (Guid playerId, IPlayerRepository playerRepository, IMoveRepository moveRepository) =>
        {
            var player = await playerRepository.GetPlayerAsync(new PlayerId(playerId));
            if (player is null)
            {
                return Results.NotFound(new
                {
                    message = "プレイヤーが見つかりません。",
                    userId = playerId
                });
            }

            var allMoves = await moveRepository.GetAllMovesAsync();
            return Results.Ok(ToPlayerResponse(player, allMoves));
        }).RequireAuthorization();

        app.MapPost("/player", async (ClaimsPrincipal user, CreatePlayerRequest request, IPlayerRepository playerRepository, ChatService chatService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var moveSet = new MoveSet();
                moveSet.SetSlot(0, new MoveId(1));

                var player = new Player(
                    playerId.Value,
                    request.UserName,
                    level: 1,
                    exp: 0,
                    status: new Status(maxHp: 24, maxMp: 8, strength: 7, defense: 5, intelligence: 5, luck: 3, speed: 4),
                    job: Job.Apprentice,
                    imagePath: PlayerImageCatalog.DefaultFileName,
                    moveSet: moveSet);
                await playerRepository.SaveAsync(player);
                await chatService.EnsureRoomAsync(player.Id);
                return Results.Ok(new
                {
                    message = "プレイヤーを作成しました。",
                    userId = player.Id.Value,
                    userName = player.Name,
                    imagePath = player.ImagePath,
                    job = new
                    {
                        code = player.Job.ToString(),
                        value = (int)player.Job,
                        displayName = EndpointHelpers.GetJobDisplayName(player.Job)
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        }).RequireAuthorization();

        app.MapPut("/player/name", async (ClaimsPrincipal user, UpdatePlayerNameRequest request, IPlayerRepository playerRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return Results.BadRequest(new { message = "ユーザー名を入力してください。" });
            }

            var normalizedUserName = request.UserName.Trim();
            if (normalizedUserName.Length > PlayerConstants.NameMaxLength)
            {
                return Results.BadRequest(new { message = $"プレイヤー名は1文字から{PlayerConstants.NameMaxLength}文字以内です." });
            }

            try
            {
                var player = await playerRepository.GetPlayerAsync(playerId.Value);
                if (player is null)
                {
                    return Results.NotFound(new
                    {
                        message = "プレイヤーが見つかりません。",
                        userId = playerId.Value.Value
                    });
                }

                player.UpdateName(normalizedUserName);
                await playerRepository.SaveAsync(player);

                return Results.Ok(new
                {
                    message = "プレイヤー情報を更新しました。",
                    userId = playerId.Value.Value,
                    userName = player.Name,
                    job = new
                    {
                        code = player.Job.ToString(),
                        value = (int)player.Job,
                        displayName = EndpointHelpers.GetJobDisplayName(player.Job)
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        }).RequireAuthorization();

        app.MapPut("/player/image", async (ClaimsPrincipal user, UpdatePlayerImageRequest request, IPlayerRepository playerRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            if (!PlayerImageCatalog.TryResolveFileName(request.ImageNo, out var fileName))
            {
                return Results.BadRequest(new
                {
                    message = $"画像番号は1から{PlayerImageCatalog.FileNames.Count}の範囲で指定してください。"
                });
            }

            try
            {
                var player = await playerRepository.GetPlayerAsync(playerId.Value);
                if (player is null)
                {
                    return Results.NotFound(new
                    {
                        message = "プレイヤーが見つかりません。",
                        userId = playerId.Value.Value
                    });
                }

                player.UpdateImagePath(fileName);
                await playerRepository.SaveAsync(player);

                return Results.Ok(new
                {
                    message = "プレイヤー画像を更新しました。",
                    userId = player.Id.Value,
                    userName = player.Name,
                    imagePath = player.ImagePath
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        }).RequireAuthorization();

        app.MapPut("/player/job", async (ClaimsPrincipal user, UpdatePlayerJobRequest request, IPlayerRepository playerRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            if (!Enum.IsDefined(request.Job))
            {
                return Results.BadRequest(new { message = "jobの値が不正です。" });
            }

            try
            {
                var player = await playerRepository.GetPlayerAsync(playerId.Value);
                if (player is null)
                {
                    return Results.NotFound(new
                    {
                        message = "プレイヤーが見つかりません。",
                        userId = playerId.Value.Value
                    });
                }

                player.UpdateJob(request.Job);
                await playerRepository.SaveAsync(player);

                return Results.Ok(new
                {
                    message = "プレイヤーの職業を更新しました。",
                    userId = playerId.Value.Value,
                    userName = player.Name,
                    job = new
                    {
                        code = player.Job.ToString(),
                        value = (int)player.Job,
                        displayName = EndpointHelpers.GetJobDisplayName(player.Job)
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        }).RequireAuthorization();

        return app;
    }

    private static object ToPlayerResponse(Player player, IReadOnlyList<Move> allMoves)
    {
        var moveById = allMoves.ToDictionary(x => x.Id.Id);
        var moveSlots = player.MoveSet.Slots
            .Select((moveId, index) =>
            {
                var move = moveId is null
                    ? null
                    : moveById.GetValueOrDefault(moveId.Id);

                return new
                {
                    slot = index + 1,
                    moveId = moveId?.Id,
                    moveName = move?.Name,
                    description = move?.Description,
                    effectImagePath = move?.EffectImagePath,
                    elementType = move?.GetOrderedEffects()
                        .FirstOrDefault(effect => effect.Damage is not null)?
                        .Damage?
                        .ElementType
                        .ToString(),
                    targetType = move?.TargetType.ToString(),
                    attackRange = move?.AttackRange.ToString(),
                    mpCost = move?.MpCost,
                    category = move?.Category.ToString()
                };
            });

        return new
        {
            userId = player.Id.Value,
            userName = player.Name,
            imagePath = player.ImagePath,
            job = new
            {
                code = player.Job.ToString(),
                value = (int)player.Job,
                displayName = EndpointHelpers.GetJobDisplayName(player.Job)
            },
            level = player.Level,
            exp = player.Exp,
            status = new
            {
                maxHp = player.Status.MaxHp,
                maxMp = player.Status.MaxMp,
                strength = player.Status.Strength,
                defense = player.Status.Defense,
                intelligence = player.Status.Intelligence,
                luck = player.Status.Luck,
                speed = player.Status.Speed
            },
            moveSlots
        };
    }
}
