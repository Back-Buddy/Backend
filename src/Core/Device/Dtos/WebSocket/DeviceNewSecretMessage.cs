using BackBuddy.Core.Library.WebSockets.Dtos;
using BackBuddy.Core.Library.WebSockets.Enums;

namespace BackBuddy.Core.Library.Device.Dtos.WebSocket
{
    public record DeviceNewSecretMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceNewSecret;
        public bool IsToSend => true;

        public required string Secret { get; set; }
    }
}
