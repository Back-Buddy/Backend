using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Device
{
    public class DeviceCreateConsumer(IDeviceService deviceService, ILogger<DeviceCreateConsumer> logger) : IConsumer<DeviceCreateRequestMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;
        private readonly ILogger<DeviceCreateConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<DeviceCreateRequestMessage> context)
        {
            _logger.LogDebug("Processing DeviceCreateRequestMessage for user: {UserId}", context.Message.UserId);

            DeviceSecretDto deviceSecret = await _deviceService.Create(context.Message.UserId, context.Message.Request);

            await context.RespondAsync(new DeviceCreateResponseMessage
            {
                DeviceSecret = deviceSecret
            });
        }
    }
}
