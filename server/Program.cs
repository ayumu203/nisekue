using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var supabaseProjectUrl = builder.Configuration["Supabase:ProjectUrl"];

var issuer = $"{supabaseProjectUrl?.TrimEnd('/')}/auth/v1";
var supabaseAudience = "authenticated";

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

    options.RequireHttpsMetadata = false;
});
builder.Services.AddAuthorization();
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("ClientCors");

app.MapGet("/", () => "Hello World!");
app.MapGet("/player", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub");
    return Results.Ok(new { userId });
}).RequireAuthorization();

app.MapPost("/player", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue("sub");
    return Results.Ok(new { message = "created", userId });
}).RequireAuthorization();

app.Run();
