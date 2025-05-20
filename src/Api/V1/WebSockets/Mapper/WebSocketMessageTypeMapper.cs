using BackBuddy.Api.Service.V1.Device.DTOs.WebSocket;
using BackBuddy.Api.Service.V1.WebSockets.Enums;

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
