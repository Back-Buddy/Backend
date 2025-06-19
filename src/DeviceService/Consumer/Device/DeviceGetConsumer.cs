using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Device
{
    public class DeviceGetConsumer(IDeviceService deviceService, ILogger<DeviceGetConsumer> logger) : IConsumer<DeviceGetRequestMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;
        private readonly ILogger<DeviceGetConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<DeviceGetRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing DeviceGetRequestMessage for user: {UserId}, device: {DeviceId}", 
                    context.Message.UserId, context.Message.DeviceId);

                DeviceDto device = await _deviceService.Get(context.Message.UserId, context.Message.DeviceId);

                await context.RespondAsync(new DeviceGetResponseMessage
                {
                    Device = device
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process DeviceGetRequestMessage");
                throw;
            }
        }
    }
}
