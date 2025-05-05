using BackBuddy.Api.Service.V1.Device.DTOs.WebSocket;
using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Device.Consumer
{
    public class DeviceNewSecretAckConsumer(IDeviceService deviceService) : IConsumer<WebSocketMessageReceive<DeviceNewSecretAckMessage>>
    {
        private readonly IDeviceService _deviceService = deviceService;

        public async Task Consume(ConsumeContext<WebSocketMessageReceive<DeviceNewSecretAckMessage>> context)
        {
            await _deviceService.AckNewSecret(context.Message.DeviceId, context.Message.Message.Secret);
        }
    }
}
