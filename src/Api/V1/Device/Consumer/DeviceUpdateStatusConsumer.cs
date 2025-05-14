using BackBuddy.Api.Service.V1.Device.DTOs.WebSocket;
using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Device.Consumer
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
