using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Enums;
using BackBuddy.Api.Service.V1.WebSockets.Mapper;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.WebSockets.Dtos
{
    public record WebSocketSendMessage
    {
        public required Guid Target { get; init; }
        public required WebSocketMessageType WebSocketMessageType { get; init; }
        public required string Payload { get; init; }
    }

    public class WebSocketSendMessageBuilder(Guid target, IWebSocketMessageDto message)
    {
        private readonly Guid _target = target;
        private readonly WebSocketMessageType _messageType = message.MessageType;
        private readonly string _payload = JsonSerializer.Serialize(message, message.MessageType.GetMessageType(), WebSocketService.JsonOptions);

        public WebSocketSendMessage Build()
        {
            return new WebSocketSendMessage
            {
                Target = _target,
                WebSocketMessageType = _messageType,
                Payload = _payload
            };
        }
    }
}
