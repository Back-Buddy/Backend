using BackBuddy.Api.Service.V1.WebSockets.Enums;
using BackBuddy.Core.Library.WebSockets.Dtos;

namespace BackBuddy.Core.Library.Device.Dtos.WebSocket
{
    public record DeviceNewSecretSetAckMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceNewSecretSetAck;
        public bool IsToSend => true;

        public required string Secret { get; set; }
    }
}
