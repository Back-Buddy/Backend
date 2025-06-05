using BackBuddy.Core.Library.Device.Dtos;
using MassTransit;

namespace BackBuddy.DeviceOrchestrator.Service.BackgroundServices
{
    public class DeviceBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<DeviceBackgroundService> logger) : BackgroundService
    {
        private readonly ILogger<DeviceBackgroundService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Device Background Service is starting...");

            using IServiceScope scope = serviceScopeFactory.CreateScope();
            IRequestClient<GetDeviceStatusesRequestMessage> requestClient = scope.ServiceProvider.GetRequiredService<IRequestClient<GetDeviceStatusesRequestMessage>>();
            IRequestClient<ValidateDeviceStatusRequestMessage> validateRequestClient = scope.ServiceProvider.GetRequiredService<IRequestClient<ValidateDeviceStatusRequestMessage>>();

            while (!stoppingToken.IsCancellationRequested)
            {
                Response<GetDeviceStatusesResponseMessage> response = await requestClient.GetResponse<GetDeviceStatusesResponseMessage>(new GetDeviceStatusesRequestMessage(), stoppingToken);
                IEnumerable<DeviceStatusDto> statuses = response.Message.StatusEntities;
                _logger.LogDebug("Received {Count} device statuses", statuses.Count());

                IEnumerable<DeviceStatusDto[]> chunks = statuses.Chunk(100);

                foreach (IEnumerable<DeviceStatusDto[]> outer_chunk in chunks.Chunk(5))
                {
                    IEnumerable<Task> tasks = outer_chunk
                        .Select(chunk => validateRequestClient.GetResponse<ValidateDeviceStatusResponseMessage>(new ValidateDeviceStatusRequestMessage { StatusEntities = chunk }));
                    await Task.WhenAll(tasks);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
