using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Mapper;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.WebSockets.Consumer
{
    public class WebSocketSendMessageConsumer(IWebSocketService webSocketService, ILogger<WebSocketSendMessageConsumer> logger) : Consumer<WebSocketSendMessage>
    {
        private readonly IWebSocketService _webSocketService = webSocketService;
        private readonly ILogger<WebSocketSendMessageConsumer> _logger = logger;

        public async Task Consume(WebSocketSendMessage message)
        {
            _logger.LogDebug("Received WebSocket message for target {Target} with type {MessageType}", message.Target, message.WebSocketMessageType);
            object rawWebSocketMessage = JsonSerializer.Deserialize(message.Payload, message.WebSocketMessageType.GetMessageType(), WebSocketService.JsonOptions) ?? throw new JsonException();

            await _webSocketService.SendMessage(message.Target, (IWebSocketMessageDto)rawWebSocketMessage);
        }
    }
}
