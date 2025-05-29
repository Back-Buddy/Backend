using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Enums;

namespace BackBuddy.Api.Service.V1.Device.DTOs.WebSocket
{
    public class DeviceUpdateStatusAckMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceUpdateStatusAck;
        public bool IsToSend => true;
    }
}
