using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Device.Consumer
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
