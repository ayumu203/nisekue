using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using server.application.chat;
using server.application.quest;
using server.application.battle;
using server.application.training;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.chat;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using server.domain.training;
using server.infrastructure;
using server.infrastructure.chat;
using System.Security.Claims;
using server.infrastructure.quest;
using server.infrastructure.quest.room;
using server.infrastructure.quest.run;
using server.infrastructure.player;
using server.infrastructure.training;
using server.infrastructure.move;
using server.shared.constants.player;

var builder = WebApplication.CreateBuilder(args);

// CORS の設定
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Supabase 周りの定義
var supabaseProjectUrl = builder.Configuration["Supabase:ProjectUrl"];
var issuer = $"{supabaseProjectUrl?.TrimEnd('/')}/auth/v1";
var supabaseAudience = builder.Configuration["Supabase:JwtAudience"] ?? "authenticated";

// 認証の設定
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MetadataAddress = $"{issuer}/.well-known/openid-configuration";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = supabaseAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),

        ValidateIssuerSigningKey = true
    };

    if (builder.Environment.IsDevelopment())
    {
        options.RequireHttpsMetadata = false;
    }
});
builder.Services.AddAuthorization();

// Supabase の接続設定
var supabaseConnectionString = builder.Configuration.GetConnectionString("Supabase")
    ?? throw new InvalidOperationException("Connection string 'Supabase' is not configured.");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(supabaseConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "internal");
    });
});

// DI
builder.Services.AddScoped<IPlayerRepository, SupabasePlayerRepository>();
builder.Services.AddSingleton<IGrowthValueRepository, CsvGrowthValueRepository>();
builder.Services.AddScoped<IChatRoomRepository, DbChatRoomRepository>();
builder.Services.AddSingleton<ITrainingEnemyRepository, CsvTrainingEnemyRepository>();
builder.Services.AddSingleton<IMoveRepository, CsvMoveRepository>();
builder.Services.AddSingleton<IQuestStageRepository, CsvQuestStageRepository>();
builder.Services.AddSingleton<IQuestEnemyDefinitionRepository, CsvQuestEnemyDefinitionRepository>();
builder.Services.AddSingleton<IQuestNpcTemplateRepository, CsvQuestNpcTemplateRepository>();
builder.Services.AddScoped<IQuestRoomRepository, DbQuestRoomRepository>();
builder.Services.AddScoped<IQuestRunRepository, DbQuestRunRepository>();
builder.Services.AddScoped<BattleService>();
builder.Services.AddScoped<QuestSnapshotFactory>();
builder.Services.AddScoped<QuestBattleFactory>();
builder.Services.AddScoped<QuestRunFactory>();
builder.Services.AddScoped<QuestNpcAssignmentService>();
builder.Services.AddScoped<QuestRoomService>();
builder.Services.AddScoped<QuestRunService>();
builder.Services.AddScoped<TrainingBattleFactory>();
builder.Services.AddScoped<TrainingOutcomeJudge>();
builder.Services.AddScoped<TrainingExpCalculator>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TrainingService>();

var app = builder.Build();

app.UseCors("ClientCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { message = "Hello World!" }));
app.MapGet("/player", async (ClaimsPrincipal user, IPlayerRepository playerRepository, IMoveRepository moveRepository) =>
{
    var playerId = TryGetPlayerId(user);
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

    return Results.Ok(new
    {
        userId = player.Id.Value,
        userName = player.Name,
        imagePath = player.ImagePath,
        job = new
        {
            code = player.Job.ToString(),
            value = (int)player.Job,
            displayName = GetJobDisplayName(player.Job)
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
    });
}).RequireAuthorization();

app.MapPost(
    "/player",
    async (ClaimsPrincipal user, CreatePlayerRequest request, IPlayerRepository playerRepository, ChatService chatService) =>
{
    var playerId = TryGetPlayerId(user);
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
            status: new Status(maxHp: 10, maxMp: 2, strength: 1, defense: 1, intelligence: 1, luck: 1, speed: 1),
            job: Job.Apprentice,
            imagePath: "ch001_bmnpc.png",
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
                displayName = GetJobDisplayName(player.Job)
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

app.MapPut(
    "/player/name",
    async (ClaimsPrincipal user, UpdatePlayerNameRequest request, IPlayerRepository playerRepository) =>
{
    var playerId = TryGetPlayerId(user);
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
                displayName = GetJobDisplayName(player.Job)
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

app.MapPut(
    "/player/job",
    async (ClaimsPrincipal user, UpdatePlayerJobRequest request, IPlayerRepository playerRepository) =>
{
    var playerId = TryGetPlayerId(user);
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
                displayName = GetJobDisplayName(player.Job)
            }
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
}).RequireAuthorization();

var questGroup = app.MapGroup("/quest").RequireAuthorization();

questGroup.MapGet("/stages", async (IQuestStageRepository questStageRepository) =>
{
    var stages = await questStageRepository.GetAllAsync();
    return Results.Ok(stages
        .Where(x => x.IsActive)
        .Select(MapQuestStage));
});

questGroup.MapPost("/rooms", async (ClaimsPrincipal user, CreateQuestRoomRequest request, QuestRoomService questRoomService) =>
{
    var playerId = TryGetPlayerId(user);
    if (playerId is null)
    {
        return Results.Unauthorized();
    }

    try
    {
        var room = await questRoomService.CreateRoomAsync(playerId.Value, new QuestStageId(request.StageId), request.Mode);
        return Results.Ok(MapQuestRoom(room));
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

questGroup.MapGet("/rooms/{roomId:guid}", async (Guid roomId, IQuestRoomRepository questRoomRepository) =>
{
    var room = await questRoomRepository.GetAsync(new QuestRoomId(roomId));
    return room is null
        ? Results.NotFound(new { message = "ルームが見つかりません。" })
        : Results.Ok(MapQuestRoom(room));
});

questGroup.MapPost("/rooms/{roomId:guid}/join", async (Guid roomId, ClaimsPrincipal user, QuestRoomService questRoomService) =>
{
    var playerId = TryGetPlayerId(user);
    if (playerId is null)
    {
        return Results.Unauthorized();
    }

    try
    {
        var room = await questRoomService.JoinRoomAsync(new QuestRoomId(roomId), playerId.Value);
        return Results.Ok(MapQuestRoom(room));
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
    IQuestRoomRepository questRoomRepository) =>
{
    var playerId = TryGetPlayerId(user);
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
        return Results.Ok(MapQuestRoom(room));
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
    QuestRoomService questRoomService) =>
{
    var playerId = TryGetPlayerId(user);
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
        return Results.Ok(MapQuestRun(run));
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

questGroup.MapGet("/runs/{runId:guid}", async (Guid runId, IQuestRunRepository questRunRepository) =>
{
    var run = await questRunRepository.GetAsync(new QuestRunId(runId));
    return run is null
        ? Results.NotFound(new { message = "クエスト進行情報が見つかりません。" })
        : Results.Ok(MapQuestRun(run));
});

questGroup.MapPost("/runs/{runId:guid}/commands", async (
    Guid runId,
    ClaimsPrincipal user,
    SubmitQuestCommandRequest request,
    IQuestRunRepository questRunRepository,
    IQuestRoomRepository questRoomRepository,
    QuestRunService questRunService) =>
{
    var playerId = TryGetPlayerId(user);
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

        var updatedRun = await questRunService.SubmitCommandAsync(run.Id, participantId, command);
        return Results.Ok(MapQuestRun(updatedRun));
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

questGroup.MapPost("/runs/{runId:guid}/resolve-timeout", async (
    Guid runId,
    ClaimsPrincipal user,
    IQuestRunRepository questRunRepository,
    IQuestRoomRepository questRoomRepository,
    QuestRunService questRunService) =>
{
    var playerId = TryGetPlayerId(user);
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
        var updatedRun = await questRunService.ResolveTimeoutAsync(run.Id, DateTimeOffset.UtcNow);
        return Results.Ok(MapQuestRun(updatedRun));
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

questGroup.MapPost("/runs/{runId:guid}/resolve-turn", async (
    Guid runId,
    ClaimsPrincipal user,
    IQuestRunRepository questRunRepository,
    IQuestRoomRepository questRoomRepository,
    QuestRunService questRunService) =>
{
    var playerId = TryGetPlayerId(user);
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
        var summary = await questRunService.ResolveTurnAsync(run.Id);
        return Results.Ok(new
        {
            turn = summary.Turn,
            isFloorCleared = summary.IsFloorCleared,
            isQuestCompleted = summary.IsQuestCompleted,
            isQuestFailed = summary.IsQuestFailed
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

questGroup.MapPost("/runs/{runId:guid}/chat", async (
    Guid runId,
    ClaimsPrincipal user,
    PostQuestChatMessageRequest request,
    IQuestRunRepository questRunRepository,
    IQuestRoomRepository questRoomRepository,
    QuestRunService questRunService) =>
{
    var playerId = TryGetPlayerId(user);
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
            participantId,
            snapshot.DisplayName,
            snapshot.ImagePath,
            request.Message,
            DateTimeOffset.UtcNow);
        var updatedRun = await questRunService.AddChatMessageAsync(run.Id, message);
        return Results.Ok(MapQuestRun(updatedRun));
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

app.MapGet("/chat/room", async (Guid ownerId, ChatService chatService) =>
{
    var room = await chatService.GetRoomAsync(new PlayerId(ownerId));

    return Results.Ok(new
    {
        ownerId = room.OwnerId.Value,
        lastChatId = room.LastChatId,
        messages = room.Messages.Select(x => new
        {
            chatId = x.ChatId,
            senderName = x.SenderName,
            text = x.Message,
            createdAt = x.CreatedAt
        })
    });
}).RequireAuthorization();

app.MapPost("/chat/room/messages", async (ClaimsPrincipal user, PostChatMessageRequest request, ChatService chatService, IPlayerRepository playerRepository) =>
{
    var currentPlayerId = TryGetPlayerId(user);
    if (currentPlayerId is null)
    {
        return Results.Unauthorized();
    }

    var ownerId = new PlayerId(request.OwnerId);
    var senderId = currentPlayerId.Value;
    var owner = await playerRepository.GetPlayerAsync(ownerId);
    if (owner is null)
    {
        return Results.NotFound(new { message = "送信先プレイヤーが見つかりません。" });
    }

    var sender = await playerRepository.GetPlayerAsync(senderId);
    if (sender is null)
    {
        return Results.NotFound(new { message = "投稿者のプレイヤーが見つかりません。" });
    }

    try
    {
        var room = await chatService.PostMessageAsync(ownerId, senderId, request.Text);
        return Results.Ok(new
        {
            ownerId = room.OwnerId.Value,
            lastChatId = room.LastChatId,
            messages = room.Messages.Select(x => new
            {
                chatId = x.ChatId,
                senderName = x.SenderName,
                text = x.Message,
                createdAt = x.CreatedAt
            })
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

app.MapGet("/training/enemies", async (TrainingService trainingService) =>
{
    var enemies = await trainingService.GetTrainingEnemies();
    return Results.Ok(enemies);
}).RequireAuthorization();

app.MapPost("/training/execute", async (ClaimsPrincipal user, ExecuteTrainingRequest request, TrainingService trainingService) =>
{
    var playerId = TryGetPlayerId(user);
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

app.Run();

static PlayerId? TryGetPlayerId(ClaimsPrincipal user)
{
    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub");

    return Guid.TryParse(subject, out var guid) ? new PlayerId(guid) : null;
}

static string GetJobDisplayName(Job job) =>
    job switch
    {
        Job.Apprentice => "見習い",
        Job.Warrior => "戦士",
        Job.Guardian => "盾使い",
        Job.Mage => "魔法使い",
        Job.Priest => "僧侶",
        Job.Ranger => "レンジャー",
        _ => job.ToString()
    };

static object MapQuestStage(QuestStageDefinition stage) => new
{
    stageId = stage.Id.Value,
    stageCode = stage.StageCode,
    name = stage.Name,
    recommendedLevel = stage.RecommendedLevel,
    minPartyMemberCount = stage.MinPartyMemberCount,
    maxPartyMemberCount = stage.MaxPartyMemberCount,
    isActive = stage.IsActive,
    floors = stage.Floors.Select(floor => new
    {
        floorNo = floor.FloorNo,
        floorType = floor.FloorType.ToString(),
        enemyCount = floor.Placements.Count
    })
};

static object MapQuestRoom(QuestRoom room) => new
{
    roomId = room.Id.Value,
    ownerPlayerId = room.OwnerId.Value,
    stageId = room.StageId.Value,
    mode = room.Mode.ToString(),
    status = room.Status.ToString(),
    version = room.Version,
    closeReason = room.CloseReason?.ToString(),
    createdAt = room.CreatedAt,
    closedAt = room.ClosedAt,
    canStart = room.CanStart(),
    formation = new
    {
        occupiedPositions = room.Formation.OccupiedPositions.Select(position => new
        {
            row = position.Row.ToString(),
            column = position.Column.ToString()
        })
    },
    participants = room.Participants.Select(participant => new
    {
        participantId = participant.Id.Value,
        type = participant.Type.ToString(),
        playerId = participant.PlayerId?.Value,
        npcTemplateId = participant.NpcTemplateId?.Value,
        displayName = participant.DisplayName,
        status = participant.Status.ToString(),
        isOwner = participant.IsOwner,
        position = new
        {
            row = participant.Position.Row.ToString(),
            column = participant.Position.Column.ToString()
        },
        joinedAt = participant.JoinedAt,
        lastSeenAt = participant.LastSeenAt,
        leftAt = participant.LeftAt
    })
};

static object MapQuestRun(QuestRun run) => new
{
    runId = run.Id.Value,
    roomId = run.RoomId.Value,
    stageId = run.StageId.Value,
    status = run.Status.ToString(),
    startedAt = run.StartedAt,
    endedAt = run.EndedAt,
    floor = new
    {
        currentFloorNo = run.FloorState.CurrentFloorNo,
        isBossFloor = run.FloorState.IsBossFloor
    },
    turn = new
    {
        currentTurnNo = run.TurnState.CurrentTurnNo,
        actionDeadlineAt = run.TurnState.ActionDeadlineAt,
        lastResolvedTurnNo = run.TurnState.LastResolvedTurnNo,
        pendingCommands = run.TurnState.PendingCommands.Select(command => new
        {
            participantId = command.ParticipantId.Value,
            turnNo = command.TurnNo,
            actionKind = command.ActionKind.ToString(),
            moveId = command.MoveId?.Id,
            target = command.SelectedTargetPosition is null
                ? null
                : new
                {
                    row = command.SelectedTargetPosition.Value.Row.ToString(),
                    column = command.SelectedTargetPosition.Value.Column.ToString()
                },
            submittedAt = command.SubmittedAt,
            isAutoSubmitted = command.IsAutoSubmitted
        })
    },
    party = run.PartySnapshots.Select(snapshot =>
    {
        var state = run.BattleState.PartyMembers.First(member => member.ParticipantId == snapshot.ParticipantId);
        return new
        {
            participantId = snapshot.ParticipantId.Value,
            type = snapshot.Type.ToString(),
            displayName = snapshot.DisplayName,
            imagePath = snapshot.ImagePath,
            job = snapshot.Job.ToString(),
            startPosition = new
            {
                row = snapshot.StartPosition.Row.ToString(),
                column = snapshot.StartPosition.Column.ToString()
            },
            currentHp = state.CurrentHp,
            currentMp = state.CurrentMp,
            isDead = state.IsDead,
            canActFromTurn = state.CanActFromTurn,
            actionMode = state.ActionMode.ToString(),
            hasLeftQuest = state.HasLeftQuest,
            isManualControlRequested = state.IsManualControlRequested,
            ailments = state.Ailments.Select(x => new
            {
                type = x.Type.ToString(),
                remainingTurns = x.RemainingTurns
            }),
            buffs = state.Buffs.Select(x => new
            {
                stat = x.Stat.ToString(),
                calculationType = x.CalculationType.ToString(),
                value = x.Value,
                remainingTurns = x.RemainingTurns
            })
        };
    }),
    enemies = run.BattleState.Enemies.Select(enemy => new
    {
        enemyInstanceId = enemy.Id.Value,
        enemyDefinitionId = enemy.EnemyDefinitionId.Value,
        position = new
        {
            row = enemy.Position.Row.ToString(),
            column = enemy.Position.Column.ToString()
        },
        currentHp = enemy.CurrentHp,
        currentMp = enemy.CurrentMp,
        isDead = enemy.IsDead,
        ailments = enemy.Ailments.Select(x => new
        {
            type = x.Type.ToString(),
            remainingTurns = x.RemainingTurns
        }),
        buffs = enemy.Buffs.Select(x => new
        {
            stat = x.Stat.ToString(),
            calculationType = x.CalculationType.ToString(),
            value = x.Value,
            remainingTurns = x.RemainingTurns
        })
    }),
    rewards = new
    {
        exp = run.Rewards.Exp
    },
    traps = run.Traps.Traps.Select(trap => new
    {
        trapId = trap.Id.Value,
        sourceParticipantId = trap.SourceParticipantId.Value,
        moveId = trap.MoveId.Id,
        expiresAfterFloorNo = trap.ExpiresAfterFloorNo,
        isTriggered = trap.IsTriggered
    }),
    chatMessages = run.ChatMessages.Select(message => new
    {
        senderParticipantId = message.SenderParticipantId.Value,
        displayName = message.DisplayName,
        imagePath = message.ImagePath,
        message = message.Message,
        sentAt = message.SentAt
    })
};

public record CreateQuestRoomRequest(int StageId, QuestRoomMode Mode);
public record UpdateQuestRoomPositionRequest(Guid ParticipantId, BattleRow Row, BattleColumn Column);
public record SubmitQuestCommandRequest(Guid ParticipantId, int TurnNo, ActionKind ActionKind, int? MoveId, BattleRow? TargetRow, BattleColumn? TargetColumn);
public record PostQuestChatMessageRequest(Guid ParticipantId, string Message);
public record CreatePlayerRequest(string UserName);
public record UpdatePlayerNameRequest(string UserName);
public record UpdatePlayerJobRequest(Job Job);
public record PostChatMessageRequest(Guid OwnerId, string Text);
public record ExecuteTrainingRequest(int EnemyId, IReadOnlyList<int?> MoveIds);
