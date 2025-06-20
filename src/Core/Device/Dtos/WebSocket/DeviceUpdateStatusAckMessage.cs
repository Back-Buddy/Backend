using BackBuddy.Core.Library.WebSockets.Dtos;
using BackBuddy.Core.Library.WebSockets.Enums;

namespace BackBuddy.Core.Library.Device.Dtos.WebSocket
{
    public class DeviceUpdateStatusAckMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceUpdateStatusAck;
        public bool IsToSend => true;
    }
}
