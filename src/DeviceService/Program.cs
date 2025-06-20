using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BackBuddy.Core.Library;
using BackBuddy.Core.Library.Database.KeyVault;
using BackBuddy.Core.Library.Database.MongoDB;
using BackBuddy.Core.Library.Database.Redis;
using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Device.Service.Consumer;
using BackBuddy.Device.Service.Consumer.Device;
using BackBuddy.Device.Service.Consumer.Report;
using BackBuddy.Device.Service.Entities;
using BackBuddy.Device.Service.Repositories;
using BackBuddy.Device.Service.Services;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using MongoDB.Driver;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#region KeyVault
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
#endregion

#region MongoDB
IConfigurationSection mongoDBSection = builder.Configuration.GetSection("MongoDB");
MongoDBConnectionConfig mongoConfig = mongoDBSection.Get<MongoDBConnectionConfig>() ?? throw new InvalidDataException("MongoDB information must be set!");

builder.Services
    .AddMongoDB(mongoConfig.Connection, mongoConfig.DatabaseName)
    .Connect()
    .AddCollection<DeviceEntity>(nameof(DeviceEntity))
    .AddCollection<DeviceLogEntity>(nameof(DeviceLogEntity))
    .AddCollection<ReportEntity>(nameof(ReportEntity))
    .AddCollection<ReportLikeEntity>(nameof(ReportLikeEntity), async collection =>
    {
        IndexKeysDefinition<ReportLikeEntity> indexKeys = new IndexKeysDefinitionBuilder<ReportLikeEntity>()
                                                .Ascending(x => x.UserId).Ascending(x => x.ReportId);
        CreateIndexModel<ReportLikeEntity> indexModel = new(indexKeys, new CreateIndexOptions { Unique = true });
        await collection.Indexes.CreateOneAsync(indexModel);

        IndexKeysDefinition<ReportLikeEntity> targetIndexKeys = new IndexKeysDefinitionBuilder<ReportLikeEntity>()
                                                        .Ascending(x => x.ReportId);
        CreateIndexModel<ReportLikeEntity> targetIndexModel = new(targetIndexKeys, new CreateIndexOptions { Unique = false });
        await collection.Indexes.CreateOneAsync(targetIndexModel);

        IndexKeysDefinition<ReportLikeEntity> userIndexKeys = new IndexKeysDefinitionBuilder<ReportLikeEntity>()
                                                        .Ascending(x => x.UserId);
        CreateIndexModel<ReportLikeEntity> userIndexModel = new(userIndexKeys, new CreateIndexOptions { Unique = false });
        await collection.Indexes.CreateOneAsync(userIndexModel);
    });
#endregion

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

builder.Services.AddTransient<IPublisher, Publisher>();

builder.Services.AddScoped<IDeviceStatusRepository, DeviceStatusRepository>();

builder.Services.AddScoped<IDeviceLogService, DeviceLogService>();
builder.Services.AddScoped<IDeviceLogRepository, DeviceLogRepository>();

builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<IReportLikeRepository, ReportLikeRepository>();
builder.Services.AddScoped<IReportLikeService, ReportLikeService>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<DeviceAuthorizeConsumer>();
    x.AddConsumer<DeviceNewSecretAckConsumer>();
    x.AddConsumer<DeviceUpdateStatusConsumer>();
    x.AddConsumer<DeviceWebSocketConnectedConsumer>();
    x.AddConsumer<GetDeviceStatusesConsumer>();
    x.AddConsumer<UserDeletedConsumer>();
    x.AddConsumer<ValidateDeviceStatusConsumer>();

    //Report Consumers
    x.AddConsumer<ReportAddLikeConsumer>();
    x.AddConsumer<ReportCreateConsumer>();
    x.AddConsumer<ReportDeleteConsumer>();
    x.AddConsumer<ReportGetConsumer>();
    x.AddConsumer<ReportGetEntityConsumer>();
    x.AddConsumer<ReportGetFeedConsumer>();
    x.AddConsumer<ReportGetLikesFromReportConsumer>();
    x.AddConsumer<ReportGetReportsConsumer>();
    x.AddConsumer<ReportGetVisibilityTypeForUserConsumer>();
    x.AddConsumer<ReportUpdateConsumer>();

    //Device CRUD
    x.AddConsumer<DeviceCreateConsumer>();
    x.AddConsumer<DeviceUpdateConsumer>();
    x.AddConsumer<DeviceDeleteConsumer>();
    x.AddConsumer<DeviceGetAllConsumer>();
    x.AddConsumer<DeviceGetConsumer>();

    //Device Log
    x.AddConsumer<DeviceGetDeviceLogConsumer>();
    x.AddConsumer<DeviceGetDeviceLogsConsumer>();


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

WebApplication app = builder.Build();
await app.RunAsync();