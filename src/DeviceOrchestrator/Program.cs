using BackBuddy.DeviceOrchestrator.Service.BackgroundServices;
using MassTransit;
using ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddHostedService<DeviceBackgroundService>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

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