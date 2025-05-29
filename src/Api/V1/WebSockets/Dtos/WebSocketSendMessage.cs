using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Enums;
using BackBuddy.Api.Service.V1.WebSockets.Mapper;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.WebSockets.Dtos
{
    public class WebSocketSendMessage
    {
        public Guid? Target { get; init; }
        public string? Payload { get; init; }
        public WebSocketMessageType? WebSocketMessageType { get; init; }

        public WebSocketSendMessage() { }

        public WebSocketSendMessage(Guid target, IWebSocketMessageDto webSocketMessage)
        {
            Target = target;
            WebSocketMessageType = webSocketMessage.MessageType;
            Payload = JsonSerializer.Serialize(webSocketMessage, webSocketMessage.MessageType.GetMessageType(), options: WebSocketService.JsonOptions);
        }
    }
}
