using BackBuddy.Core.Library.Device.Dtos.WebSocket;
using BackBuddy.Core.Library.WebSockets.Dtos;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer
{
    public class DeviceUpdateStatusConsumer(IDeviceService deviceService) : IConsumer<WebSocketMessageReceive<DeviceUpdateStatusMessage>>
    {
        private readonly IDeviceService _deviceService = deviceService;

        public async Task Consume(ConsumeContext<WebSocketMessageReceive<DeviceUpdateStatusMessage>> context)
        {
            await _deviceService.HandleStatusUpdate(context.Message.DeviceId, context.Message.Message, context.CancellationToken);
        }
    }
}
