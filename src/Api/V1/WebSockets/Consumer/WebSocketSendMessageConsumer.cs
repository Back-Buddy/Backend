using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Mapper;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using MassTransit;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.WebSockets.Consumer
{
    public class WebSocketSendMessageConsumer(IWebSocketService webSocketService) : IConsumer<WebSocketSendMessage>
    {
        private readonly IWebSocketService _webSocketService = webSocketService;

        public async Task Consume(ConsumeContext<WebSocketSendMessage> context)
        {
            WebSocketSendMessage message = context.Message;
            object rawWebSocketMessage = JsonSerializer.Deserialize(message.Payload, message.WebSocketMessageType.GetMessageType(), WebSocketService.JsonOptions) ?? throw new JsonException();
            await _webSocketService.SendMessage(message.Target, (IWebSocketMessageDto)rawWebSocketMessage);
        }
    }
}
