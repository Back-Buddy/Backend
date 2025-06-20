using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Device
{
    public class DeviceGetAllConsumer(IDeviceService deviceService, ILogger<DeviceGetAllConsumer> logger) : IConsumer<DeviceGetAllRequestMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;
        private readonly ILogger<DeviceGetAllConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<DeviceGetAllRequestMessage> context)
        {
            _logger.LogDebug("Processing DeviceGetAllRequestMessage for user: {UserId}", context.Message.UserId);

            Page<List<DeviceDto>> devices = await _deviceService.GetAll(
                context.Message.UserId,
                context.Message.Page,
                context.Message.Query);

            await context.RespondAsync(new DeviceGetAllResponseMessage
            {
                Devices = devices
            });
        }
    }
}
