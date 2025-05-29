
using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Mapper;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.WebSockets.BackgroundServices
{
    public class WebSocketMessageConsumer(IServiceScopeFactory serviceScopeFactory, IConnectionMultiplexer connection, ILogger<WebSocketMessageConsumer> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly IConnectionMultiplexer _connection = connection;
        private readonly ILogger<WebSocketMessageConsumer> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ISubscriber subscriber = _connection.GetSubscriber();

            await subscriber.SubscribeAsync(new RedisChannel(typeof(WebSocketSendMessage).FullName ?? typeof(WebSocketSendMessage).Name, RedisChannel.PatternMode.Literal), async (channel, message) =>
            {
                if (message.IsNullOrEmpty) return;
                try
                {
                    using IServiceScope scope = _serviceScopeFactory.CreateScope();

                    WebSocketSendMessage webSocketMessage = JsonSerializer.Deserialize<WebSocketSendMessage>(message!, WebSocketService.JsonOptions) ?? throw new JsonException();
                    _logger.LogDebug("Received WebSocket message for target {Target} with type {MessageType}", webSocketMessage.Target, webSocketMessage.WebSocketMessageType);

                    IWebSocketService webSocketService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
                    object rawWebSocketMessage = JsonSerializer.Deserialize(webSocketMessage.Payload, webSocketMessage.WebSocketMessageType.GetMessageType(), WebSocketService.JsonOptions) ?? throw new JsonException();

                    await webSocketService.SendMessage(webSocketMessage.Target, (IWebSocketMessageDto)rawWebSocketMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
                    return;
                }
            });
        }
    }
}
