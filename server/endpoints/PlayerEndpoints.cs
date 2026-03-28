using System.Security.Claims;
using server.application.chat;
using server.application.player;
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

        app.MapGet("/player", async (
            ClaimsPrincipal user,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IEquipmentRepository equipmentRepository,
            IMoveRepository moveRepository,
            IJobProfileRepository jobProfileRepository,
            EquipmentStatusResolver equipmentStatusResolver) =>
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
            var playerEquipments = await playerEquipmentRepository.GetByPlayerAsync(player.Id);
            var equipments = await equipmentRepository.GetAllAsync();
            return Results.Ok(ToPlayerResponse(player, allMoves, jobProfileRepository, playerEquipments, equipments, equipmentStatusResolver));
        }).RequireAuthorization();

        app.MapGet("/players/{playerId:guid}", async (
            Guid playerId,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IEquipmentRepository equipmentRepository,
            IMoveRepository moveRepository,
            IJobProfileRepository jobProfileRepository,
            EquipmentStatusResolver equipmentStatusResolver) =>
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
            var playerEquipments = await playerEquipmentRepository.GetByPlayerAsync(player.Id);
            var equipments = await equipmentRepository.GetAllAsync();
            return Results.Ok(ToPlayerResponse(player, allMoves, jobProfileRepository, playerEquipments, equipments, equipmentStatusResolver));
        }).RequireAuthorization();

        app.MapPost("/player", async (
            ClaimsPrincipal user,
            CreatePlayerRequest request,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IEquipmentRepository equipmentRepository,
            ChatService chatService,
            IJobProfileRepository jobProfileRepository) =>
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
                    jobLevel: 1,
                    jobExp: 0,
                    status: new Status(maxHp: 24, maxMp: 8, strength: 7, defense: 5, intelligence: 5, luck: 3, speed: 4),
                    job: Job.Apprentice,
                    imagePath: PlayerImageCatalog.DefaultFileName,
                    moveSet: moveSet);
                await playerRepository.SaveAsync(player);
                var equipments = await equipmentRepository.GetAllAsync();
                await playerEquipmentRepository.SaveAsync(CreateStarterEquipments(player, equipments, DateTimeOffset.UtcNow));
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
                        displayName = EndpointHelpers.GetJobDisplayName(player.Job),
                        description = jobProfileRepository.GetByJob(player.Job).Description
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

        app.MapPut("/player/equipment", async (
            ClaimsPrincipal user,
            UpdatePlayerEquipmentRequest request,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IEquipmentRepository equipmentRepository,
            IMoveRepository moveRepository,
            IJobProfileRepository jobProfileRepository,
            EquipmentStatusResolver equipmentStatusResolver) =>
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

            var playerEquipments = (await playerEquipmentRepository.GetByPlayerAsync(player.Id)).ToList();
            var equipments = await equipmentRepository.GetAllAsync();
            var equipmentById = equipments.ToDictionary(x => x.Id);
            var now = DateTimeOffset.UtcNow;

            foreach (var equipped in playerEquipments.Where(x => x.Type == request.EquipmentType && x.Status == EquipmentStatus.Equipped))
            {
                equipped.Unequip(now);
            }

            if (request.PlayerEquipmentId is not null)
            {
                var target = playerEquipments.FirstOrDefault(x => x.Id == new PlayerEquipmentId(request.PlayerEquipmentId.Value));
                if (target is null)
                {
                    return Results.NotFound(new { message = "指定された装備個体が見つかりません。" });
                }

                if (target.Type != request.EquipmentType)
                {
                    return Results.BadRequest(new { message = "装備スロットと装備種別が一致しません。" });
                }

                if (!equipmentById.TryGetValue(target.EquipmentId, out var equipment))
                {
                    return Results.BadRequest(new { message = "装備マスタが見つかりません。" });
                }

                try
                {
                    target.Equip(equipment, player.Job, now);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }

            await playerEquipmentRepository.SaveAsync(playerEquipments);
            var allMoves = await moveRepository.GetAllMovesAsync();

            return Results.Ok(new
            {
                message = "装備を更新しました。",
                player = ToPlayerResponse(player, allMoves, jobProfileRepository, playerEquipments, equipments, equipmentStatusResolver)
            });
        }).RequireAuthorization();

        app.MapPut("/player/name", async (ClaimsPrincipal user, UpdatePlayerNameRequest request, IPlayerRepository playerRepository, IJobProfileRepository jobProfileRepository) =>
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
                        displayName = EndpointHelpers.GetJobDisplayName(player.Job),
                        description = jobProfileRepository.GetByJob(player.Job).Description
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

        app.MapPut("/player/job", async (
            ClaimsPrincipal user,
            UpdatePlayerJobRequest request,
            PlayerJobService playerJobService,
            IJobProfileRepository jobProfileRepository) =>
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
                var result = await playerJobService.ChangeJobAsync(playerId.Value, request.Job);
                var player = result.Player;

                return Results.Ok(new
                {
                    message = "プレイヤーの職業を更新しました。",
                    userId = playerId.Value.Value,
                    userName = player.Name,
                    job = new
                    {
                        code = player.Job.ToString(),
                        value = (int)player.Job,
                        displayName = EndpointHelpers.GetJobDisplayName(player.Job),
                        description = jobProfileRepository.GetByJob(player.Job).Description
                    },
                    newlyLearnedMoves = result.NewlyLearnedMoves.Select(move => new
                    {
                        moveId = move.MoveId,
                        moveName = move.MoveName
                    })
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message, userId = playerId.Value.Value });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        }).RequireAuthorization();

        return app;
    }

    private static object ToPlayerResponse(
        Player player,
        IReadOnlyList<Move> allMoves,
        IJobProfileRepository jobProfileRepository,
        IReadOnlyList<PlayerEquipment> playerEquipments,
        IReadOnlyList<Equipment> equipments,
        EquipmentStatusResolver equipmentStatusResolver)
    {
        var moveById = allMoves.ToDictionary(x => x.Id.Id);
        var equipmentById = equipments.ToDictionary(x => x.Id);
        var jobProfile = jobProfileRepository.GetByJob(player.Job);
        var effectiveStatus = equipmentStatusResolver.BuildEffectiveStatus(player.Status, player.Job, playerEquipments, equipments);
        var jobProfiles = jobProfileRepository.GetAll()
            .Select(profile => new
            {
                code = profile.Job.ToString(),
                value = (int)profile.Job,
                displayName = EndpointHelpers.GetJobDisplayName(profile.Job),
                description = profile.Description
            })
            .ToArray();
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
        var equipmentItems = playerEquipments
            .Select(playerEquipment =>
            {
                if (!equipmentById.TryGetValue(playerEquipment.EquipmentId, out var equipment))
                {
                    return null;
                }

                return new
                {
                    playerEquipmentId = playerEquipment.Id.Value,
                    equipmentId = equipment.Id.Value,
                    name = equipment.Name,
                    equipmentType = equipment.Type.ToString(),
                    status = playerEquipment.Status.ToString(),
                    durability = playerEquipment.Durability,
                    maxDurability = equipment.MaxDurability,
                    mastery = playerEquipment.Mastery,
                    canEquipCurrentJob = equipment.CanEquip(player.Job),
                    bonusValues = new
                    {
                        maxHp = equipment.BonusValues.MaxHp,
                        maxMp = equipment.BonusValues.MaxMp,
                        strength = equipment.BonusValues.Strength,
                        defense = equipment.BonusValues.Defense,
                        intelligence = equipment.BonusValues.Intelligence,
                        luck = equipment.BonusValues.Luck,
                        speed = equipment.BonusValues.Speed
                    }
                };
            })
            .OfType<object>()
            .ToArray();

        return new
        {
            userId = player.Id.Value,
            userName = player.Name,
            imagePath = player.ImagePath,
            job = new
            {
                code = player.Job.ToString(),
                value = (int)player.Job,
                displayName = EndpointHelpers.GetJobDisplayName(player.Job),
                description = jobProfile.Description
            },
            jobProfiles,
            level = player.Level,
            exp = player.Exp,
            jobLevel = player.JobLevel,
            jobExp = player.JobExp,
            status = new
            {
                baseValues = new
                {
                    maxHp = player.Status.MaxHp,
                    maxMp = player.Status.MaxMp,
                    strength = player.Status.Strength,
                    defense = player.Status.Defense,
                    intelligence = player.Status.Intelligence,
                    luck = player.Status.Luck,
                    speed = player.Status.Speed
                },
                effectiveValues = new
                {
                    maxHp = effectiveStatus.MaxHp,
                    maxMp = effectiveStatus.MaxMp,
                    strength = effectiveStatus.Strength,
                    defense = effectiveStatus.Defense,
                    intelligence = effectiveStatus.Intelligence,
                    luck = effectiveStatus.Luck,
                    speed = effectiveStatus.Speed
                }
            },
            moveSlots,
            equipments = equipmentItems
        };
    }

    private static IReadOnlyList<PlayerEquipment> CreateStarterEquipments(Player player, IReadOnlyList<Equipment> equipments, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(equipments);

        var equipmentById = equipments.ToDictionary(x => x.Id);

        return
        [
            CreateStarterEquipment(player, equipmentById, new EquipmentId(1001), EquipmentType.Weapon, now),
            CreateStarterEquipment(player, equipmentById, new EquipmentId(2001), EquipmentType.Armor, now)
        ];
    }

    private static PlayerEquipment CreateStarterEquipment(
        Player player,
        IReadOnlyDictionary<EquipmentId, Equipment> equipmentById,
        EquipmentId equipmentId,
        EquipmentType equipmentType,
        DateTimeOffset now)
    {
        if (!equipmentById.TryGetValue(equipmentId, out var equipment))
        {
            throw new InvalidOperationException($"初期装備マスタが見つかりません。 equipmentId={equipmentId.Value}");
        }

        var playerEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            player.Id,
            equipmentId,
            equipmentType,
            EquipmentStatus.Inventory,
            durability: equipment.MaxDurability,
            mastery: 0,
            acquiredAt: now,
            updatedAt: now);
        playerEquipment.Equip(equipment, player.Job, now);
        return playerEquipment;
    }
}
