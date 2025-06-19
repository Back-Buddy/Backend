using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Device
{
    public class DeviceDeleteConsumer(IDeviceService deviceService, ILogger<DeviceDeleteConsumer> logger) : IConsumer<DeviceDeleteRequestMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;
        private readonly ILogger<DeviceDeleteConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<DeviceDeleteRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing DeviceDeleteRequestMessage for user: {UserId}, device: {DeviceId}",
                    context.Message.UserId, context.Message.DeviceId);

                await _deviceService.Delete(context.Message.UserId, context.Message.DeviceId);

                await context.RespondAsync(new DeviceDeleteResponseMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process DeviceDeleteRequestMessage");
                throw;
            }
        }
    }
}
