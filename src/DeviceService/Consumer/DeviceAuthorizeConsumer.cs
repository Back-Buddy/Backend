using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer
{
    public class DeviceAuthorizeConsumer(IDeviceService deviceService) : IConsumer<DeviceAuthorizeRequestMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;

        public async Task Consume(ConsumeContext<DeviceAuthorizeRequestMessage> context)
        {
            DeviceDto device = await _deviceService.Authorize(context.Message.Secret);
            await context.RespondAsync(device);
        }
    }
}
