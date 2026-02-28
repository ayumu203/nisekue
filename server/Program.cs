var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("TestMessage", async () =>
{
    TestMessage testMessage = new TestMessage("うにくえ は にせくえ に 30 のダメージを与えた.\n");
    return testMessage.Message;
});

app.Run();
