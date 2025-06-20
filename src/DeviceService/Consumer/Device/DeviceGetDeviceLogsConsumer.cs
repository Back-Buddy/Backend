using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Device
{
    public class DeviceGetDeviceLogsConsumer(IDeviceLogService deviceLogService, ILogger<DeviceGetDeviceLogsConsumer> logger) : IConsumer<DeviceGetDeviceLogsRequestMessage>
    {
        private readonly IDeviceLogService _deviceLogService = deviceLogService;
        private readonly ILogger<DeviceGetDeviceLogsConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<DeviceGetDeviceLogsRequestMessage> context)
        {
            _logger.LogDebug("Processing DeviceGetDeviceLogsRequestMessage for user: {UserId}, device: {DeviceId}",
                context.Message.UserId, context.Message.DeviceId);

            Page<List<DeviceLogDto>> deviceLogs = await _deviceLogService.GetDeviceLogs(
                context.Message.UserId,
                context.Message.DeviceId,
                context.Message.Query,
                context.Message.Page);

            await context.RespondAsync(new DeviceGetDeviceLogsResponseMessage
            {
                DeviceLogs = deviceLogs
            });
        }
    }
}