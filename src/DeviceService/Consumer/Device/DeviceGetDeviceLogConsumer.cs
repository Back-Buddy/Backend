using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Device
{
    public class DeviceGetDeviceLogConsumer(IDeviceLogService deviceLogService, ILogger<DeviceGetDeviceLogConsumer> logger) : IConsumer<DeviceGetDeviceLogRequestMessage>
    {
        private readonly IDeviceLogService _deviceLogService = deviceLogService;
        private readonly ILogger<DeviceGetDeviceLogConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<DeviceGetDeviceLogRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing DeviceGetDeviceLogRequestMessage for user: {UserId}, device: {DeviceId}, log: {LogId}",
                    context.Message.UserId, context.Message.DeviceId, context.Message.LogId);

                DeviceLogDto deviceLog = await _deviceLogService.GetDeviceLog(
                    context.Message.UserId,
                    context.Message.DeviceId,
                    context.Message.LogId);

                await context.RespondAsync(new DeviceGetDeviceLogResponseMessage
                {
                    DeviceLog = deviceLog
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process DeviceGetDeviceLogRequestMessage");
                throw;
            }
        }
    }
}
