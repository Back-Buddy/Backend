using BackBuddy.Core.Library.Device.Dtos.WebSocket;
using BackBuddy.Core.Library.WebSockets.Enums;

namespace BackBuddy.Core.Library.WebSockets.Mapper
{
    public static class WebSocketMessageTypeMapper
    {
        public static Type GetMessageType(this WebSocketMessageType type)
        {
            return type switch
            {
                WebSocketMessageType.DeviceNewSecret => typeof(DeviceNewSecretMessage),
                WebSocketMessageType.DeviceNewSecretAck => typeof(DeviceNewSecretAckMessage),
                WebSocketMessageType.DeviceNewSecretSetAck => typeof(DeviceNewSecretSetAckMessage),
                WebSocketMessageType.DeviceUpdateStatus => typeof(DeviceUpdateStatusMessage),
                WebSocketMessageType.DeviceUpdateStatusAck => typeof(DeviceUpdateStatusAckMessage),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
