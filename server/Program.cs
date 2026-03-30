using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using System.Linq;
using server.application.battle;
using server.application.chat;
using server.application.maintenance;
using server.application.quest;
using server.application.training;
using server.application.player;
using server.domain.chat;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.training;
using server.endpoints;
using server.infrastructure;
using server.infrastructure.chat;
using server.infrastructure.move;
using server.infrastructure.player;
using server.infrastructure.quest;
using server.infrastructure.quest.room;
using server.infrastructure.quest.run;
using server.infrastructure.training;

var builder = WebApplication.CreateBuilder(args);

var defaultCorsAllowedOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:5173", "https://ayumu203.github.io" }
    : new[] { "https://game.arm203.org" };

var configuredCorsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

var corsAllowedOrigins = configuredCorsAllowedOrigins is { Length: > 0 }
    ? configuredCorsAllowedOrigins
    : defaultCorsAllowedOrigins;

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy
            .WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var supabaseProjectUrl = builder.Configuration["Supabase:ProjectUrl"];
var issuer = $"{supabaseProjectUrl?.TrimEnd('/')}/auth/v1";
var supabaseAudience = builder.Configuration["Supabase:JwtAudience"] ?? "authenticated";

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
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/quest-hubs/runs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };

    if (builder.Environment.IsDevelopment())
    {
        options.RequireHttpsMetadata = false;
    }
});

builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSignalR();

var supabaseConnectionString = builder.Configuration.GetConnectionString("Supabase")
    ?? throw new InvalidOperationException("Connection string 'Supabase' is not configured.");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(supabaseConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "internal");
    });
});

builder.Services.AddScoped<IPlayerRepository, SupabasePlayerRepository>();
builder.Services.AddScoped<IPlayerEquipmentRepository, DbPlayerEquipmentRepository>();
builder.Services.AddScoped<IPlayerItemStackRepository, DbPlayerItemStackRepository>();
builder.Services.AddSingleton<IEquipmentRepository, CsvEquipmentRepository>();
builder.Services.AddSingleton<IItemRepository, CsvItemRepository>();
builder.Services.AddScoped<IMarketListingRepository, DbMarketListingRepository>();
builder.Services.AddScoped<IMarketTradeHistoryRepository, DbMarketTradeHistoryRepository>();
builder.Services.AddScoped<IItemDeletionLogRepository, DbItemDeletionLogRepository>();
builder.Services.AddSingleton<IJobProfileRepository, CsvJobProfileRepository>();
builder.Services.AddScoped<IChatRoomRepository, DbChatRoomRepository>();
builder.Services.AddScoped<IThreadRepository, DbThreadRepository>();
builder.Services.AddSingleton<ITrainingEnemyRepository, CsvTrainingEnemyRepository>();
builder.Services.AddSingleton<IMoveRepository, CsvMoveRepository>();
builder.Services.AddSingleton<IJobMoveLearningRuleRepository, CsvJobMoveLearningRuleRepository>();
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
builder.Services.AddScoped<QuestResponseMapper>();
builder.Services.AddHostedService<QuestRunTimeoutBackgroundService>();
builder.Services.AddScoped<TrainingBattleFactory>();
builder.Services.AddScoped<TrainingOutcomeJudge>();
builder.Services.AddScoped<TrainingExpCalculator>();
builder.Services.AddScoped<TrainingWeaponMasteryPolicy>();
builder.Services.AddScoped<EquipmentStatusResolver>();
builder.Services.AddScoped<PlayerJobService>();
builder.Services.AddScoped<PlayerRebirthService>();
builder.Services.AddScoped<MarketListingCleanupService>();
builder.Services.AddScoped<DevelopmentDataCleanupService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ThreadService>();
builder.Services.AddScoped<TrainingService>();

var app = builder.Build();

app.UseCors("ClientCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { message = "Hello World!" }));
app.MapPlayerEndpoints();
app.MapItemEndpoints();
app.MapQuestEndpoints();
app.MapChatEndpoints();
app.MapThreadEndpoints();
app.MapTrainingEndpoints();

app.Run();
