using System.Text;
using BackBuddy.Api.Service.V1.Database.MongoDB;
using BackBuddy.Core.Library.Database.Firebase;
using BackBuddy.User.Service.Consumers;
using BackBuddy.User.Service.Entities;
using BackBuddy.User.Service.Repositories;
using BackBuddy.User.Service.Services;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using MassTransit;
using MongoDB.Driver;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#region MongoDB
IConfigurationSection mongoDBSection = builder.Configuration.GetSection("MongoDB");
MongoDBConnectionConfig mongoConfig = mongoDBSection.Get<MongoDBConnectionConfig>() ?? throw new InvalidDataException("MongoDB information must be set!");

builder.Services
    .AddMongoDB(mongoConfig.Connection, mongoConfig.DatabaseName)
    .Connect()
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

    builder.Services.AddSingleton(firestoreDb);
}
#endregion

builder.Services.AddScoped<IUserRelationRepository, UserRelationRepository>();
builder.Services.AddScoped<IUserRelationService, UserRelationService>();
builder.Services.AddScoped<IUserService, UserService>();


builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<GetFcmTokensConsumer>();
    x.AddConsumer<GetStrongFollowRelationsAndAllFollowingsConsumer>();
    x.AddConsumer<GetUserConsumer>();
    x.AddConsumer<HasUserStrongRelationConsumer>();
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

WebApplication app = builder.Build();
await app.RunAsync();