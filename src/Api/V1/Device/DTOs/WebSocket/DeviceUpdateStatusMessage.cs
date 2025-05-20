using BackBuddy.Api.Service.V1.Device.Enums;
using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Enums;

namespace BackBuddy.Api.Service.V1.Device.DTOs.WebSocket
{
    public class DeviceUpdateStatusMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceUpdateStatus;

        public UserPositionStatusType UserPositionStatus { get; init; }

        public bool IsToSend() => false;
    }
}
