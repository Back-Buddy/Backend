using BackBuddy.Core.Library.WebSockets.Dtos;
using BackBuddy.Core.Library.WebSockets.Enums;

namespace BackBuddy.Core.Library.Device.Dtos.WebSocket
{
    public class DeviceNewSecretAckMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceNewSecretAck;
        public bool IsToSend => false;

        public required string Secret { get; set; }
    }
}
