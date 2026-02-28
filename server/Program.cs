var builder = WebApplication.CreateBuilder(args);
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

var app = builder.Build();

app.UseCors("ClientCors");

app.MapGet("/", () => "Hello World!");
app.MapGet("/api/test-message", () =>
{
    return Results.Ok(new TestMessage("うにくえ は にせくえ に 30 のダメージを与えた."));
});

app.Run();
