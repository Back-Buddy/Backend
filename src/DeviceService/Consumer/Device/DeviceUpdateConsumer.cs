using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Device
{
    public class DeviceUpdateConsumer(IDeviceService deviceService, ILogger<DeviceUpdateConsumer> logger) : IConsumer<DeviceUpdateRequestMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;
        private readonly ILogger<DeviceUpdateConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<DeviceUpdateRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing DeviceUpdateRequestMessage for user: {UserId}, device: {DeviceId}", 
                    context.Message.UserId, context.Message.DeviceId);

                await _deviceService.Update(context.Message.UserId, context.Message.DeviceId, context.Message.Request);

                await context.RespondAsync(new DeviceUpdateResponseMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process DeviceUpdateRequestMessage");
                throw;
            }
        }
    }
}
