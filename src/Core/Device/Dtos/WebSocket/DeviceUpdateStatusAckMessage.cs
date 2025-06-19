using BackBuddy.Api.Service.V1.WebSockets.Enums;
using BackBuddy.Core.Library.WebSockets.Dtos;

namespace BackBuddy.Core.Library.Device.Dtos.WebSocket
{
    public class DeviceUpdateStatusAckMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceUpdateStatusAck;
        public bool IsToSend => true;
    }
}
