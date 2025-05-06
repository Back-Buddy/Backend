using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Queue;
using BackBuddy.Api.Service.V1.Device.Services;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Device.Consumer
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
