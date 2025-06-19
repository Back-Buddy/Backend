using BackBuddy.Core.Library.WebSockets.Dtos;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer
{
    public class DeviceWebSocketConnectedConsumer(IDeviceService deviceService) : IConsumer<WebSocketConnectedMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;

        public async Task Consume(ConsumeContext<WebSocketConnectedMessage> context)
        {
            await _deviceService.TryUpdateSecret(context.Message.DeviceId);
        }
    }
}
