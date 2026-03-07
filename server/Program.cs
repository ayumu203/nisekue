using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using server.application.chat;
using server.application.training;
using server.domain.chat;
using server.domain.player;
using server.domain.training;
using server.infrastructure;
using server.infrastructure.chat;
using System.Security.Claims;
using server.infrastructure.player;
using server.infrastructure.training;

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
builder.Services.AddScoped<IChatRoomRepository, DbChatRoomRepository>();
builder.Services.AddSingleton<ITrainingEnemyRepository, CsvTrainingEnemyRepository>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TrainingService>();

var app = builder.Build();

app.UseCors("ClientCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { message = "Hello World!" }));
app.MapGet("/player", async (ClaimsPrincipal user, IPlayerRepository playerRepository) =>
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

    return Results.Ok(new
    {
        userId = player.Id.Value,
        userName = player.Name,
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
        }
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
        var player = new Player(
            playerId.Value,
            request.UserName,
            level: 1,
            exp: 0,
            status: new Status(maxHp: 1, maxMp: 0, strength: 0, defense: 0, intelligence: 0, luck: 0, speed: 0));
        await playerRepository.SaveAsync(player);
        await chatService.EnsureRoomAsync(player.Id);
        return Results.Ok(new
        {
            message = "プレイヤーを作成しました。",
            userId = player.Id.Value,
            userName = player.Name
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
        var result = await trainingService.ExecuteTraining(playerId.Value, new TrainingEnemyId(request.EnemyId));
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
}).RequireAuthorization();

app.Run();

static PlayerId? TryGetPlayerId(ClaimsPrincipal user)
{
    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub");

    return Guid.TryParse(subject, out var guid) ? new PlayerId(guid) : null;
}

public record CreatePlayerRequest(string UserName);
public record PostChatMessageRequest(Guid OwnerId, string Text);
public record ExecuteTrainingRequest(int EnemyId);
