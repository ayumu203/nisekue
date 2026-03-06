using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using server.domain.chat;
using server.domain.player;
using server.infrastructure;
using server.infrastructure.chat;
using System.Security.Claims;
using server.infrastructure.player;

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

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(supabaseConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "internal");
    });
});

// DI
builder.Services.AddScoped<IPlayerRepository, SupabasePlayerRepository>();
builder.Services.AddScoped<IChatRoomRepository, DbChatRoomRepository>();

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

app.MapPost("/player", async (ClaimsPrincipal user, CreatePlayerRequest request, IPlayerRepository playerRepository) =>
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
            status: new BaseStatus(maxHp: 1, maxMp: 0, strength: 0, defense: 0, intelligence: 0, luck: 0, speed: 0));
        await playerRepository.SaveAsync(player);
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

app.Run();

static PlayerId? TryGetPlayerId(ClaimsPrincipal user)
{
    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub");

    return Guid.TryParse(subject, out var guid) ? new PlayerId(guid) : null;
}

public record CreatePlayerRequest(string UserName);
