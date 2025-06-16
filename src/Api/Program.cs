using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BackBuddy.Api.Service;
using BackBuddy.Api.Service.Swagger;
using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Database.Firebase;
using BackBuddy.Api.Service.V1.Database.KeyVault;
using BackBuddy.Api.Service.V1.Database.MongoDB;
using BackBuddy.Api.Service.V1.Database.Redis;
using BackBuddy.Api.Service.V1.Device.Consumer;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.ExceptionHandlers;
using BackBuddy.Api.Service.V1.Notifications.Consumers;
using BackBuddy.Api.Service.V1.Notifications.Services;
using BackBuddy.Api.Service.V1.Users.Consumers;
using BackBuddy.Api.Service.V1.Users.Entities;
using BackBuddy.Api.Service.V1.Users.Repositories;
using BackBuddy.Api.Service.V1.Users.Services;
using BackBuddy.Api.Service.V1.WebSockets.BackgroundServices;
using BackBuddy.Api.Service.V1.WebSockets.Consumer;
using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.Middleware;
using BackBuddy.Api.Service.V1.WebSockets.Repositories;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using MongoDB.Driver;
using StackExchange.Redis;
using System.Text;
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
    .AddCollection<ReportEntity>(nameof(ReportEntity))
    .AddCollection<UserFollowEntity>(nameof(UserFollowEntity), async collection =>
    {
        IndexKeysDefinition<UserFollowEntity> indexKeys = new IndexKeysDefinitionBuilder<UserFollowEntity>()
                                                        .Ascending(x => x.UserId).Ascending(x => x.TargetId);
        CreateIndexModel<UserFollowEntity> indexModel = new(indexKeys, new CreateIndexOptions { Unique = true });
        await collection.Indexes.CreateOneAsync(indexModel);

        IndexKeysDefinition<UserFollowEntity> targetIndexKeys = new IndexKeysDefinitionBuilder<UserFollowEntity>()
                                                        .Ascending(x => x.TargetId);
        CreateIndexModel<UserFollowEntity> targetIndexModel = new(targetIndexKeys, new CreateIndexOptions { Unique = false });
        await collection.Indexes.CreateOneAsync(targetIndexModel);

        IndexKeysDefinition<UserFollowEntity> userIndexKeys = new IndexKeysDefinitionBuilder<UserFollowEntity>()
                                                        .Ascending(x => x.UserId);
        CreateIndexModel<UserFollowEntity> userIndexModel = new(userIndexKeys, new CreateIndexOptions { Unique = false });
        await collection.Indexes.CreateOneAsync(userIndexModel);
    })
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

#region Firebase
IConfigurationSection firebaseSection = builder.Configuration.GetSection("Firebase");

if (!builder.Environment.IsDevelopment())
{
    FirebaseConfig firebaseConfig = firebaseSection.Get<FirebaseConfig>() ?? throw new InvalidDataException("Firebase information must be set!");
    GoogleCredential googleCredential = GoogleCredential.FromJson(Encoding.UTF8.GetString(Convert.FromBase64String(firebaseConfig.Secret)));

    if (FirebaseApp.DefaultInstance == null)
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = googleCredential,
            ProjectId = firebaseConfig.ProjectId
        });
    }

    FirestoreDb firestoreDb = await new FirestoreDbBuilder
    {
        Credential = googleCredential,
        ProjectId = firebaseConfig.ProjectId,
    }.BuildAsync();

    builder.Services.AddSingleton(FirebaseMessaging.DefaultInstance);
    builder.Services.AddSingleton(firestoreDb);
    builder.Services.AddSingleton<INotificationService, NotificationService>();
}
else
{
    FirebaseDevConfig firebaseDevConfig = firebaseSection.Get<FirebaseDevConfig>() ?? throw new InvalidDataException("Firebase development information must be set!");
    builder.Services.AddSingleton(firebaseDevConfig);

    Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", firebaseDevConfig.FireStoreEmulatorHost);
    Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", firebaseDevConfig.FireAuthEmulatorHost);

    if (FirebaseApp.DefaultInstance == null)
    {
        FirebaseApp.Create(new AppOptions
        {
            ProjectId = firebaseDevConfig.ProjectId,
            Credential = GoogleCredential.FromAccessToken("test")
        });
    }

    FirestoreDb firestoreDb = await new FirestoreDbBuilder
    {
        ProjectId = firebaseDevConfig.ProjectId,
        EmulatorDetection = EmulatorDetection.EmulatorOnly,
    }.BuildAsync();

    builder.Services.AddSingleton(FirebaseMessaging.DefaultInstance);
    builder.Services.AddSingleton(firestoreDb);

    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<INotificationService, DevNotificationService>();
}
#endregion

builder.Services.AddScoped<IUserRelationRepository, UserRelationRepository>();
builder.Services.AddScoped<IUserRelationService, UserRelationService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IDeviceStatusRepository, DeviceStatusRepository>();

builder.Services.AddScoped<IDeviceLogService, DeviceLogService>();
builder.Services.AddScoped<IDeviceLogRepository, DeviceLogRepository>();

builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IWebSocketService, WebSocketService>();

builder.Services.AddSingleton<IConnectedDeviceRepository, ConnectedDeviceRepository>();

builder.Services.AddScoped<IReportLikeRepository, ReportLikeRepository>();
builder.Services.AddScoped<IReportLikeService, ReportLikeService>();

builder.Services.AddSingleton<ConnectedDeviceHeartbeatService>();

builder.Services.AddHostedService(x =>
{
    return x.GetRequiredService<ConnectedDeviceHeartbeatService>();
});

builder.Services.AddOptions<ConnectedDeviceConfig>()
    .Bind(builder.Configuration.GetSection("ConnectedDeviceConfig"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddTransient<IPublisher, Publisher>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<DeviceWebSocketConnectedConsumer>();
    x.AddConsumer<DeviceNewSecretAckConsumer>();
    x.AddConsumer<DeviceAuthorizeConsumer>();
    x.AddConsumer<DeviceUpdateStatusConsumer>();
    x.AddConsumer<GetDeviceStatusesConsumer>();
    x.AddConsumer<ValidateDeviceStatusConsumer>();
    x.AddConsumer<SendNotificationConsumer>();
    x.AddConsumer<GetFcmTokensConsumer>();
    x.AddConsumer<UserDeletedConsumer>();
    x.AddConsumer<GetUserConsumer>();
    x.AddConsumer<UserFollowedConsumer>();

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

app.MapControllers()
    .RequireAuthorization();

app.UseExceptionHandler();
app.UseMiddleware<CustomWebSocketMiddleware>();

await app.RunAsync();
