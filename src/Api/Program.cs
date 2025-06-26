using BackBuddy.Api.Service.Swagger;
using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.ExceptionHandlers;
using BackBuddy.Api.Service.V1.WebSockets.BackgroundServices;
using BackBuddy.Api.Service.V1.WebSockets.Consumer;
using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.Middleware;
using BackBuddy.Api.Service.V1.WebSockets.Repositories;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using BackBuddy.Core.Library.Database.Redis;
using BackBuddy.Core.Library.WebSockets.Dtos;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ServiceDefaults;
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.ConfigureAuthentification();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AbstractBaseExceptionHandler>();

#region Redis
IConfigurationSection redisSection = builder.Configuration.GetSection("Redis");
RedisConnectionConfig redisConfig = redisSection.Get<RedisConnectionConfig>() ?? throw new InvalidDataException("Redis information must be set!");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    return ConnectionMultiplexer.Connect(redisConfig.Connection);
});

builder.Services.AddSingleton<IDistributedCache>(sp =>
{
    IConnectionMultiplexer multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
    var options = new RedisCacheOptions
    {
        ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer),
        InstanceName = redisConfig.DatabaseName
    };
    return new RedisCache(options);
});
#endregion

builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IWebSocketService, WebSocketService>();
builder.Services.AddSingleton<IConnectedDeviceRepository, ConnectedDeviceRepository>();
builder.Services.AddSingleton<ConnectedDeviceHeartbeatService>();

builder.Services.AddHostedService(x =>
{
    return x.GetRequiredService<ConnectedDeviceHeartbeatService>();
});

builder.Services.AddOptions<ConnectedDeviceConfig>()
    .Bind(builder.Configuration.GetSection("ConnectedDeviceConfig"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<WebSocketDeviceIsOnlineConsumer>();

    string connection = builder.Configuration.GetValue<string>($"MESSAGE_QUEUE_CONNECTION") ?? throw new InvalidOperationException("MESSAGE_QUEUE_CONNECTION is not set!");
    if (builder.Environment.IsDevelopment())
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(connection);
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(connection);
            cfg.ConfigureEndpoints(context);
        });
    }
});

builder.Services.AddRedisSub()
    .AddConsumer<WebSocketSendMessageConsumer, WebSocketSendMessage>()
    .Build();

builder.Services.ConfigureFullSwaggerConfig();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(5)
});

app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.MapDefaultHealthCheckEndpoints();

app.MapControllers()
    .RequireAuthorization();

app.UseExceptionHandler();
app.UseMiddleware<CustomWebSocketMiddleware>();

await app.RunAsync();
