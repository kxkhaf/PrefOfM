using MusicService.API.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureServices();

var app = builder.Build();
app.ConfigureMiddleware();
app.ConfigureEndpoints();

app.Run();