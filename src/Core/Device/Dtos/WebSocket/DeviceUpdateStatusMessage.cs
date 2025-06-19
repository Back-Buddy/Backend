using BackBuddy.Api.Service.V1.WebSockets.Enums;
using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Core.Library.WebSockets.Dtos;

namespace BackBuddy.Core.Library.Device.Dtos.WebSocket
{
    public class DeviceUpdateStatusMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.DeviceUpdateStatus;
        public bool IsToSend => false;

        public UserPositionStatusType UserPositionStatus { get; init; }
    }
}
