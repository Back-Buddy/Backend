using BackBuddy.Core.Library.Database.Firebase;
using BackBuddy.Notification.Service.Consumers;
using BackBuddy.Notification.Service.Services;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using MassTransit;
using ServiceDefaults;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

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

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<SendNotificationConsumer>();

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
app.MapDefaultHealthCheckEndpoints();
await app.RunAsync();