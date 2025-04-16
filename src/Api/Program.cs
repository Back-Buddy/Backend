using BackBuddy.Api.Service.Swagger;
using BackBuddy.Api.Service.V1.Auth;
using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.ExceptionHandlers;
using BackBuddy.Api.Service.V1.WebSockets.Middleware;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureAuthentification();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AbstractBaseExceptionHandler>();

builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IWebSocketService, WebSocketService>();
builder.Services.AddScoped<CustomWebSocketMiddleware>();

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

app.UseExceptionHandler();
app.UseMiddleware<CustomWebSocketMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(5)
});

await app.RunAsync();
