using BackBuddy.Notification.Emulator.Endpoints;
using BackBuddy.Notification.Emulator.Services;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();

builder.Services.AddSingleton<INotificationService, NotificationService>();

WebApplication app = builder.Build();

app.MapOpenApi();
#pragma warning disable S1075 // URIs should not be hardcoded
app.MapScalarApiReference(opt => opt.Servers = [new ScalarServer("http://localhost:8083")]);
#pragma warning restore S1075 // URIs should not be hardcoded

app.MapNotificationEndpoints();

await app.RunAsync();