using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BackBuddy.Api.Service;
using BackBuddy.Api.Service.Swagger;
using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Database.KeyVault;
using BackBuddy.Api.Service.V1.Database.MongoDB;
using BackBuddy.Api.Service.V1.Database.Redis;
using BackBuddy.Api.Service.V1.Device.Consumer;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.ExceptionHandlers;
using BackBuddy.Api.Service.V1.WebSockets.Middleware;
using BackBuddy.Api.Service.V1.WebSockets.Repositories;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using MassTransit;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.ConfigureAuthentification();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AbstractBaseExceptionHandler>();

if (!builder.Environment.IsDevelopment())
{
    SecretClient secretClient = new(new Uri(builder.Configuration.GetValue<string>("KEY_VAULT_URI") ?? throw new InvalidDataException("KEY_VAULT_URI is not set!")), new DefaultAzureCredential());
    builder.Services.AddKeyedSingleton(Constants.DEVICE_SECRET, secretClient);
    builder.Services.AddSingleton<ISecretProvider, KeyVaultSecretProvider>();
}
else
{
    builder.Services.AddSingleton<ISecretProvider, DevSecretProvider>();
}

#region MongoDB
IConfigurationSection mongoDBSection = builder.Configuration.GetSection("MongoDB");
MongoDBConnectionConfig mongoConfig = mongoDBSection.Get<MongoDBConnectionConfig>() ?? throw new InvalidDataException("MongoDB information must be set!");

builder.Services
    .AddMongoDB(mongoConfig.Connection, mongoConfig.DatabaseName)
    .Connect()
    .AddCollection<DeviceEntity>(nameof(DeviceEntity))
    .AddCollection<DeviceLogEntity>(nameof(DeviceLogEntity))
    .AddCollection<ReportEntity>(nameof(ReportEntity));
#endregion

#region Redis
IConfigurationSection redisSection = builder.Configuration.GetSection("Redis");
RedisConnectionConfig redisConfig = redisSection.Get<RedisConnectionConfig>() ?? throw new InvalidDataException("Redis information must be set!");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfig.Connection;
    options.InstanceName = redisConfig.DatabaseName;
});
#endregion
builder.Services.AddScoped<IDeviceStatusRepository, DeviceStatusRepository>();

builder.Services.AddScoped<IDeviceLogService, DeviceLogService>();
builder.Services.AddScoped<IDeviceLogRepository, DeviceLogRepository>();

builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IWebSocketService, WebSocketService>();

builder.Services.AddScoped<IConnectedDeviceRepository, ConnectedDeviceRepository>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<DeviceWebSocketConnectedConsumer>();
    x.AddConsumer<DeviceNewSecretAckConsumer>();
    x.AddConsumer<DeviceAuthorizeConsumer>();
    x.AddConsumer<DeviceUpdateStatusConsumer>();

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

app.MapControllers()
    .RequireAuthorization();

app.UseExceptionHandler();
app.UseMiddleware<CustomWebSocketMiddleware>();

await app.RunAsync();
