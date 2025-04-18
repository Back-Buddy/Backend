using BackBuddy.Api.Service.Swagger;
using BackBuddy.Api.Service.V1.Auth;
using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Database.MongoDB;
using BackBuddy.Api.Service.V1.ExceptionHandlers;
using BackBuddy.Api.Service.V1.WebSockets.Middleware;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureAuthentification();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AbstractBaseExceptionHandler>();

IConfigurationSection mongoDBSection = builder.Configuration.GetSection("MongoDB");
MongoDBConnectionConfig mongoConfig = mongoDBSection.Get<MongoDBConnectionConfig>() ?? throw new InvalidDataException("MongoDB information must be set!");

builder.Services
    .AddMongoDB()
    .AddConnection(mongoConfig.Connection)
    .AddDatabaseName(mongoConfig.DatabaseName)
    .Connect();

builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IWebSocketService, WebSocketService>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    //TODO: Register your consumers here
    x.AddConsumer<TestConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();

builder.Services.ConfigureFullSwaggerConfig();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(5)
});

app.UseExceptionHandler();
app.UseMiddleware<CustomWebSocketMiddleware>();

await app.RunAsync();
