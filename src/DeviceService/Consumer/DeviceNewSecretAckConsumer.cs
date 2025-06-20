using BackBuddy.Core.Library.Device.Dtos.WebSocket;
using BackBuddy.Core.Library.WebSockets.Dtos;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer
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
