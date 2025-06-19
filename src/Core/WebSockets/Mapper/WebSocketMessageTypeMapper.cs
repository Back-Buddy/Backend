using BackBuddy.Api.Service.V1.WebSockets.Enums;
using BackBuddy.Core.Library.Device.Dtos.WebSocket;

namespace BackBuddy.Api.Service.V1.WebSockets.Mapper
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
