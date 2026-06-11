using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using server.application.chat;
using server.application.player;
using server.domain.move;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.player;
using server.shared.pagination;
using server.shared.constants.player;

namespace server.endpoints;

internal static class PlayerEndpoints
{
    internal static WebApplication MapPlayerEndpoints(this WebApplication app)
    {
        app.MapGet("/players", async (
            int? page,
            int? pageSize,
            int? limit,
            IPlayerRepository playerRepository,
            IJobProfileRepository jobProfileRepository,
            CombatIndexCalculator combatIndexCalculator,
            CombatIndexRankEvaluator combatIndexRankEvaluator) =>
        {
            if (!PaginationQueryResolver.TryResolve(page, pageSize, limit, out var offset, out var effectiveLimit, out var errorMessage))
            {
                return Results.BadRequest(new { message = errorMessage });
            }

            var players = await playerRepository.GetAllAsync(offset, effectiveLimit);

            return Results.Ok(players.Select(player => new
            {
                userId = player.Id.Value,
                userName = player.Name,
                imagePath = player.ImagePath,
                level = player.Level,
                job = new
                {
                    code = player.Job.ToString(),
                    value = (int)player.Job,
                    displayName = EndpointHelpers.GetJobDisplayName(player.Job),
                    description = jobProfileRepository.GetByJob(player.Job).Description
                },
                combatIndexRank = combatIndexRankEvaluator.Evaluate(combatIndexCalculator.Calculate(player.Status)).ToString()
            }));
        }).RequireAuthorization();

        app.MapGet("/player", async (
            ClaimsPrincipal user,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IEquipmentRepository equipmentRepository,
            IMoveRepository moveRepository,
            PlayerMoveSetSanitizer playerMoveSetSanitizer,
            IJobProfileRepository jobProfileRepository,
            EquipmentStatusResolver equipmentStatusResolver,
            StatusRankEvaluator statusRankEvaluator,
            CombatIndexCalculator combatIndexCalculator,
            CombatIndexRankEvaluator combatIndexRankEvaluator,
            AppDbContext db) =>
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
            if (playerMoveSetSanitizer.TrySanitize(player, allMoves))
            {
                await playerRepository.SaveAsync(player);
            }

            var now = DateTimeOffset.UtcNow;
            var lastActiveThreshold = now.AddMinutes(-5);
            await db.Players
                .Where(p => p.Id == playerId.Value.Value && (p.LastActiveAt == null || p.LastActiveAt < lastActiveThreshold))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastActiveAt, now));

            var playerEquipments = await playerEquipmentRepository.GetByPlayerAsync(player.Id);
            var equipments = await equipmentRepository.GetAllAsync();
            return Results.Ok(ToPlayerResponse(
                player,
                allMoves,
                jobProfileRepository,
                playerEquipments,
                equipments,
                equipmentStatusResolver,
                statusRankEvaluator,
                combatIndexCalculator,
                combatIndexRankEvaluator));
        }).RequireAuthorization();

        app.MapGet("/players/{playerId:guid}", async (
            Guid playerId,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IEquipmentRepository equipmentRepository,
            IMoveRepository moveRepository,
            PlayerMoveSetSanitizer playerMoveSetSanitizer,
            IJobProfileRepository jobProfileRepository,
            EquipmentStatusResolver equipmentStatusResolver,
            StatusRankEvaluator statusRankEvaluator,
            CombatIndexCalculator combatIndexCalculator,
            CombatIndexRankEvaluator combatIndexRankEvaluator) =>
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
            if (playerMoveSetSanitizer.TrySanitize(player, allMoves))
            {
                await playerRepository.SaveAsync(player);
            }
            var playerEquipments = await playerEquipmentRepository.GetEquippedByPlayerAsync(player.Id);
            var equipments = await equipmentRepository.GetAllAsync();
            return Results.Ok(ToPlayerResponse(
                player,
                allMoves,
                jobProfileRepository,
                playerEquipments,
                equipments,
                equipmentStatusResolver,
                statusRankEvaluator,
                combatIndexCalculator,
                combatIndexRankEvaluator));
        }).RequireAuthorization();

        app.MapPost("/player", async (
            ClaimsPrincipal user,
            CreatePlayerRequest request,
            IPlayerRepository playerRepository,
            IPlayerEquipmentRepository playerEquipmentRepository,
            IPlayerItemStackRepository playerItemStackRepository,
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
                moveSet.SetSlot(0, new MoveId(101));

                var player = new Player(
                    playerId.Value,
                    request.UserName,
                    rebirthCount: 0,
                    level: 1,
                    exp: 0,
                    jobLevel: 1,
                    jobExp: 0,
                    gold: 100,
                    status: new Status(maxHp: 24, maxMp: 8, strength: 7, defense: 5, intelligence: 5, luck: 3, speed: 4),
                    job: Job.Apprentice,
                    imagePath: PlayerImageCatalog.DefaultFileName,
                    moveSet: moveSet);
                var now = DateTimeOffset.UtcNow;
                await playerRepository.SaveAsync(player);
                var equipments = await equipmentRepository.GetAllAsync();
                await playerEquipmentRepository.SaveAsync(CreateStarterEquipments(player, equipments, now));
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
            EquipmentStatusResolver equipmentStatusResolver,
            StatusRankEvaluator statusRankEvaluator,
            CombatIndexCalculator combatIndexCalculator,
            CombatIndexRankEvaluator combatIndexRankEvaluator) =>
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
                player = ToPlayerResponse(
                    player,
                    allMoves,
                    jobProfileRepository,
                    playerEquipments,
                    equipments,
                    equipmentStatusResolver,
                    statusRankEvaluator,
                    combatIndexCalculator,
                    combatIndexRankEvaluator)
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

        app.MapPut("/player/move-set", async (
            ClaimsPrincipal user,
            UpdatePlayerMoveSetRequest request,
            PlayerMoveSetService playerMoveSetService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                await playerMoveSetService.UpdateAsync(playerId.Value, request.MoveIds);
                return Results.Ok(new
                {
                    message = "スキル順を更新しました。"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
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

        app.MapPost("/players/{targetPlayerId:guid}/gifts", async (
            ClaimsPrincipal user,
            Guid targetPlayerId,
            SendPlayerGiftRequest request,
            IDbContextFactory<AppDbContext> dbContextFactory,
            PlayerForUpdateLockService playerForUpdateLockService,
            IItemRepository itemRepository,
            IEquipmentRepository equipmentRepository,
            ChatService chatService) =>
        {
            var senderId = EndpointHelpers.TryGetPlayerId(user);
            if (senderId is null)
            {
                return Results.Unauthorized();
            }

            var sendingEquipment = request.PlayerEquipmentId is not null;
            var sendingItem = request.ItemStackId is not null;
            if (sendingEquipment == sendingItem)
            {
                return Results.BadRequest(new { message = "装備かアイテムのどちらか一方を指定してください。" });
            }

            if (senderId.Value.Value == targetPlayerId)
            {
                return Results.BadRequest(new { message = "自分自身にはプレゼントできません。" });
            }

            var quantity = request.Quantity ?? 1;
            if (request.ItemStackId is not null && quantity <= 0)
            {
                return Results.BadRequest(new { message = "送信数は1以上で指定してください。" });
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await using var tx = await dbContext.Database.BeginTransactionAsync();

            var lockedPlayerIds = new[] { senderId.Value.Value, targetPlayerId }
                .Distinct()
                .ToArray();
            IReadOnlyDictionary<Guid, PlayerEntity> lockedPlayers;
            try
            {
                lockedPlayers = await playerForUpdateLockService.LockPlayersAsync(dbContext, lockedPlayerIds);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }

            var sender = lockedPlayers[senderId.Value.Value];
            var recipient = lockedPlayers[targetPlayerId];
            var now = DateTimeOffset.UtcNow;
            string giftItemName;
            int giftQuantity;

            if (request.PlayerEquipmentId is not null)
            {
                var equipmentEntity = await dbContext.PlayerEquipments
                    .FromSqlInterpolated($"SELECT * FROM internal.player_equipments WHERE id = {request.PlayerEquipmentId.Value} FOR UPDATE")
                    .SingleOrDefaultAsync();
                if (equipmentEntity is null || equipmentEntity.PlayerId != sender.Id)
                {
                    return Results.NotFound(new { message = "送信対象の装備が見つかりません。" });
                }

                if ((EquipmentStatus)equipmentEntity.EquipmentStatus == EquipmentStatus.Equipped)
                {
                    return Results.BadRequest(new { message = "装備中アイテムはプレゼントできません。" });
                }

                var isListedForMarket = await dbContext.MarketListings
                    .Where(x => x.PlayerEquipmentId == equipmentEntity.Id && x.ExpiresAt > now && x.RemainingQuantity > 0)
                    .AnyAsync();
                if (isListedForMarket)
                {
                    return Results.BadRequest(new { message = "出品中の装備はプレゼントできません。" });
                }

                if (await CalculateUsedSlotsForPlayerAsync(dbContext, recipient.Id) + 1 > 20)
                {
                    return Results.UnprocessableEntity(new { message = "受信者の所持枠が不足しています。" });
                }

                var equipment = MapToDomain(equipmentEntity);
                equipment.TransferOwnership(new PlayerId(recipient.Id), now);
                ApplyPlayerEquipmentEntity(equipmentEntity, equipment);

                var equipmentMaster = await equipmentRepository.GetAsync(new EquipmentId(equipmentEntity.EquipmentId));
                giftItemName = equipmentMaster?.Name ?? "装備";
                giftQuantity = 1;
            }
            else
            {
                if (request.ItemStackId is null)
                {
                    return Results.BadRequest(new { message = "送信対象のアイテムが見つかりません。" });
                }

                var senderStackEntity = await dbContext.PlayerItemStacks
                    .FromSqlInterpolated($"SELECT * FROM internal.player_item_stacks WHERE id = {request.ItemStackId.Value} FOR UPDATE")
                    .SingleOrDefaultAsync();
                if (senderStackEntity is null || senderStackEntity.PlayerId != sender.Id)
                {
                    return Results.NotFound(new { message = "送信対象のアイテムスタックが見つかりません。" });
                }

                var item = await itemRepository.GetAsync(new ItemId(senderStackEntity.ItemId));
                if (item is null)
                {
                    return Results.BadRequest(new { message = "アイテムマスタが見つかりません。" });
                }

                var senderStack = MapToDomain(senderStackEntity);
                try
                {
                    senderStack.ConsumeQuantity(quantity, now);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }

                var recipientStackEntity = await dbContext.PlayerItemStacks
                    .SingleOrDefaultAsync(x => x.PlayerId == recipient.Id && x.ItemId == item.Id.Value);

                if (recipientStackEntity is null)
                {
                    if (await CalculateUsedSlotsForPlayerAsync(dbContext, recipient.Id) + 1 > 20)
                    {
                        return Results.UnprocessableEntity(new { message = "受信者の所持枠が不足しています。" });
                    }

                    dbContext.PlayerItemStacks.Add(new PlayerItemStackEntity
                    {
                        Id = Guid.NewGuid(),
                        PlayerId = recipient.Id,
                        ItemId = item.Id.Value,
                        Quantity = quantity,
                        UpdatedAt = now
                    });
                }
                else
                {
                    var recipientStack = MapToDomain(recipientStackEntity);
                    if (recipientStack.Quantity + quantity > item.MaxStack)
                    {
                        return Results.UnprocessableEntity(new { message = "受信者のスタック上限を超えるため送信できません。" });
                    }

                    recipientStack.AddQuantity(quantity, item.MaxStack, now);
                    ApplyPlayerItemStackEntity(recipientStackEntity, recipientStack);
                }

                if (senderStack.Quantity == 0)
                {
                    dbContext.PlayerItemStacks.Remove(senderStackEntity);
                }
                else
                {
                    ApplyPlayerItemStackEntity(senderStackEntity, senderStack);
                }

                giftItemName = item.Name;
                giftQuantity = quantity;
            }

            await dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            await chatService.PostSystemMessageAsync(new PlayerId(recipient.Id), $"{sender.Name} から {giftItemName} x{giftQuantity} を受け取りました。");

            return Results.Ok(new
            {
                message = "プレゼントを送信しました。"
            });
        }).RequireAuthorization();

        app.MapPost("/player/rebirth", async (
            ClaimsPrincipal user,
            PlayerRebirthService playerRebirthService,
            IJobProfileRepository jobProfileRepository) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var player = await playerRebirthService.RebirthAsync(playerId.Value);

                return Results.Ok(new
                {
                    message = "転生しました。",
                    userId = player.Id.Value,
                    userName = player.Name,
                    rebirthCount = player.RebirthCount,
                    job = new
                    {
                        code = player.Job.ToString(),
                        value = (int)player.Job,
                        displayName = EndpointHelpers.GetJobDisplayName(player.Job),
                        description = jobProfileRepository.GetByJob(player.Job).Description
                    },
                    level = player.Level,
                    exp = player.Exp,
                    requiredExpForNextLevel = player.RequiredExpForNextLevel(),
                    jobLevel = player.JobLevel,
                    jobExp = player.JobExp,
                    requiredJobExpForNextLevel = player.RequiredJobExpForNextLevel(),
                    gold = player.Gold,
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
                        }
                    }
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

        app.MapGet("/player/job-roadmap/list", async (
            ClaimsPrincipal user,
            JobRoadmapService jobRoadmapService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var list = await jobRoadmapService.GetListAsync(playerId.Value);
                return Results.Ok(list);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message, userId = playerId.Value.Value });
            }
        }).RequireAuthorization();

        app.MapGet("/player/job-roadmap", async (
            ClaimsPrincipal user,
            int job,
            JobRoadmapService jobRoadmapService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            if (!Enum.IsDefined(typeof(Job), job))
            {
                return Results.BadRequest(new { message = "jobの値が不正です。" });
            }

            try
            {
                var roadmap = await jobRoadmapService.GetRoadmapAsync(playerId.Value, (Job)job);
                return Results.Ok(roadmap);
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

        app.MapPut("/player/job-roadmap/unlock", async (
            ClaimsPrincipal user,
            UnlockJobRoadmapRequest request,
            JobRoadmapService jobRoadmapService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            if (!Enum.IsDefined(typeof(Job), request.JobId))
            {
                return Results.BadRequest(new { message = "jobIdの値が不正です。" });
            }

            try
            {
                var result = await jobRoadmapService.UnlockAsync(playerId.Value, (Job)request.JobId);
                return Results.Ok(new
                {
                    jobId = result.JobId,
                    jobName = result.JobName,
                    paidGold = result.PaidGold,
                    remainingGold = result.RemainingGold
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
        EquipmentStatusResolver equipmentStatusResolver,
        StatusRankEvaluator statusRankEvaluator,
        CombatIndexCalculator combatIndexCalculator,
        CombatIndexRankEvaluator combatIndexRankEvaluator)
    {
        var moveById = allMoves.ToDictionary(x => x.Id.Id);
        var equipmentById = equipments.ToDictionary(x => x.Id);
        var jobProfile = jobProfileRepository.GetByJob(player.Job);
        var effectiveStatus = equipmentStatusResolver.BuildEffectiveStatus(player.Status, player.Job, playerEquipments, equipments);
        var baseCombatIndex = combatIndexCalculator.Calculate(player.Status);
        var effectiveCombatIndex = combatIndexCalculator.Calculate(effectiveStatus);
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

                var effects = move?.GetOrderedEffects();

                return new
                {
                    slot = index + 1,
                    moveId = moveId?.Id,
                    moveName = move?.Name,
                    description = move?.Description,
                    effectImagePath = move?.EffectImagePath,
                    elementType = effects?
                        .FirstOrDefault(effect => effect.Damage is not null)?
                        .Damage?
                        .ElementType
                        .ToString(),
                    targetType = move?.TargetType.ToString(),
                    targetLifeState = move?.TargetLifeState.ToString(),
                    attackRange = move?.AttackRange.ToString(),
                    mpCost = move?.MpCost,
                    category = move?.Category.ToString(),
                    effectSummaries = effects?
                        .Select(effect => new
                        {
                            effectType = effect.EffectType.ToString(),
                            powerRate = effect.Damage?.PowerRate,
                            buffStat = effect.Buff?.BuffStat.ToString(),
                            buffTurns = effect.Buff?.BuffTurns,
                            ailmentType = effect.Ailment?.AilmentType.ToString(),
                            ailmentTurns = effect.Ailment?.AilmentTurns,
                        })
                        .ToArray()
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
                    flavorText = equipment.FlavorText,
                    equipmentType = equipment.Type.ToString(),
                    status = playerEquipment.Status.ToString(),
                    plusValue = playerEquipment.PlusValue,
                    mastery = playerEquipment.Mastery,
                    masteryCap = equipment.MasteryCap,
                    synthesisGoldCost = equipment.SynthesisGoldCost,
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
            rebirthCount = player.RebirthCount,
            job = new
            {
                code = player.Job.ToString(),
                value = (int)player.Job,
                displayName = EndpointHelpers.GetJobDisplayName(player.Job),
                description = jobProfile.Description
            },
            jobProfiles,
            masteredJobs = player.MasteredJobs
                .OrderBy(job => (int)job)
                .Select(job => new
                {
                    code = job.ToString(),
                    value = (int)job,
                    displayName = EndpointHelpers.GetJobDisplayName(job),
                    description = jobProfileRepository.GetByJob(job).Description
                })
                .ToArray(),
            level = player.Level,
            exp = player.Exp,
            jobLevel = player.JobLevel,
            jobExp = player.JobExp,
            gold = player.Gold,
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
                baseRanks = BuildStatusRankResponse(player.Status, statusRankEvaluator),
                effectiveValues = new
                {
                    maxHp = effectiveStatus.MaxHp,
                    maxMp = effectiveStatus.MaxMp,
                    strength = effectiveStatus.Strength,
                    defense = effectiveStatus.Defense,
                    intelligence = effectiveStatus.Intelligence,
                    luck = effectiveStatus.Luck,
                    speed = effectiveStatus.Speed
                },
                effectiveRanks = BuildStatusRankResponse(effectiveStatus, statusRankEvaluator)
            },
            combatIndex = new
            {
                baseValue = baseCombatIndex,
                baseRank = combatIndexRankEvaluator.Evaluate(baseCombatIndex).ToString(),
                effectiveValue = effectiveCombatIndex,
                effectiveRank = combatIndexRankEvaluator.Evaluate(effectiveCombatIndex).ToString()
            },
            moveSlots,
            equipments = equipmentItems
        };
    }

    private static object BuildStatusRankResponse(Status status, StatusRankEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(evaluator);

        return new
        {
            maxHp = evaluator.Evaluate(RankingStatusKeys.MaxHp, status.MaxHp).ToString(),
            maxMp = evaluator.Evaluate(RankingStatusKeys.MaxMp, status.MaxMp).ToString(),
            strength = evaluator.Evaluate(RankingStatusKeys.Strength, status.Strength).ToString(),
            defense = evaluator.Evaluate(RankingStatusKeys.Defense, status.Defense).ToString(),
            intelligence = evaluator.Evaluate(RankingStatusKeys.Intelligence, status.Intelligence).ToString(),
            luck = evaluator.Evaluate(RankingStatusKeys.Luck, status.Luck).ToString(),
            speed = evaluator.Evaluate(RankingStatusKeys.Speed, status.Speed).ToString()
        };
    }

    private static async Task<int> CalculateUsedSlotsForPlayerAsync(AppDbContext dbContext, Guid playerId)
    {
        var listedEquipmentIds = await dbContext.MarketListings
            .AsNoTracking()
            .Where(x => x.SellerId == playerId && x.ExpiresAt > DateTimeOffset.UtcNow && x.RemainingQuantity > 0 && x.PlayerEquipmentId != null)
            .Select(x => x.PlayerEquipmentId!.Value)
            .ToHashSetAsync();
        var equipmentCount = await dbContext.PlayerEquipments
            .AsNoTracking()
            .CountAsync(x => x.PlayerId == playerId && x.EquipmentStatus != (int)EquipmentStatus.Equipped && !listedEquipmentIds.Contains(x.Id));
        var stackCount = await dbContext.PlayerItemStacks
            .AsNoTracking()
            .CountAsync(x => x.PlayerId == playerId);

        return equipmentCount + stackCount;
    }

    private static PlayerEquipment MapToDomain(PlayerEquipmentEntity entity)
    {
        return new PlayerEquipment(
            new PlayerEquipmentId(entity.Id),
            new PlayerId(entity.PlayerId),
            new EquipmentId(entity.EquipmentId),
            (EquipmentType)entity.EquipmentType,
            (EquipmentStatus)entity.EquipmentStatus,
            entity.Durability,
            entity.Mastery,
            entity.PlusValue,
            entity.AcquiredAt,
            entity.UpdatedAt);
    }

    private static void ApplyPlayerEquipmentEntity(PlayerEquipmentEntity entity, PlayerEquipment equipment)
    {
        entity.PlayerId = equipment.PlayerId.Value;
        entity.EquipmentStatus = (int)equipment.Status;
        entity.Durability = equipment.Durability;
        entity.Mastery = equipment.Mastery;
        entity.PlusValue = equipment.PlusValue;
        entity.UpdatedAt = equipment.UpdatedAt;
    }

    private static PlayerItemStack MapToDomain(PlayerItemStackEntity entity)
    {
        return new PlayerItemStack(
            new PlayerItemStackId(entity.Id),
            new PlayerId(entity.PlayerId),
            new ItemId(entity.ItemId),
            entity.Quantity,
            entity.UpdatedAt);
    }

    private static void ApplyPlayerItemStackEntity(PlayerItemStackEntity entity, PlayerItemStack stack)
    {
        entity.Quantity = stack.Quantity;
        entity.UpdatedAt = stack.UpdatedAt;
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
            plusValue: 0,
            acquiredAt: now,
            updatedAt: now);
        playerEquipment.Equip(equipment, player.Job, now);
        return playerEquipment;
    }

}
