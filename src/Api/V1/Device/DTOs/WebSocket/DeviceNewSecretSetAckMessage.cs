using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Enums;

namespace BackBuddy.Api.Service.V1.Device.DTOs.WebSocket
{
    public record DeviceNewSecretSetAckMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceNewSecretSetAck;
        public bool IsToSend => true;

        public required string Secret { get; set; }
    }
}
