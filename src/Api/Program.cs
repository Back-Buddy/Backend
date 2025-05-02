using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BackBuddy.Api.Service;
using BackBuddy.Api.Service.Swagger;
using BackBuddy.Api.Service.V1.Auth;
using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Database.KeyVault;
using BackBuddy.Api.Service.V1.Database.MongoDB;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.ExceptionHandlers;
using BackBuddy.Api.Service.V1.WebSockets.Middleware;
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
    SecretClient secretClient = new (new Uri(builder.Configuration.GetValue<string>("KEY_VAULT_URI") ?? throw new InvalidDataException("KEY_VAULT_URI is not set!")), new DefaultAzureCredential());
    builder.Services.AddKeyedSingleton(Constants.DEVICE_SECRET, secretClient);
    builder.Services.AddSingleton<ISecretProvider, KeyVaultSecretProvider>();
}
else
{
    builder.Services.AddSingleton<ISecretProvider, DevSecretProvider>();
}

    IConfigurationSection mongoDBSection = builder.Configuration.GetSection("MongoDB");
MongoDBConnectionConfig mongoConfig = mongoDBSection.Get<MongoDBConnectionConfig>() ?? throw new InvalidDataException("MongoDB information must be set!");

builder.Services
    .AddMongoDB(mongoConfig.Connection, mongoConfig.DatabaseName)
    .Connect()
    .AddCollection<DeviceEntity>(nameof(DeviceEntity));

builder.Services.AddTransient<IDeviceRepository, DeviceRepository>();
builder.Services.AddTransient<IDeviceService, DeviceService>();

builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IWebSocketService, WebSocketService>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    //TODO: Register your consumers here

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

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
