
using BackBuddy.Api.Service.V1.Utilities;
using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.Repositories;
using Microsoft.Extensions.Options;

namespace BackBuddy.Api.Service.V1.WebSockets.BackgroundServices
{
    public class ConnectedDeviceHeartbeatService(IConnectedDeviceRepository connectedDeviceRepository, IOptions<ConnectedDeviceConfig> options, ILogger<ConnectedDeviceHeartbeatService> logger) : BackgroundService
    {
        private readonly IConnectedDeviceRepository _connectedDeviceRepository = connectedDeviceRepository;
        private readonly ConnectedDeviceConfig _connectedDeviceConfig = options.Value;
        private readonly ILogger<ConnectedDeviceHeartbeatService> _logger = logger;

        private readonly ConcurrentList<Guid> _connectedDevices = new();

        public void AddConnectedDevice(Guid deviceId)
        {
            _connectedDevices.Add(deviceId);
        }

        public void RemoveConnectedDevice(Guid deviceId)
        {
            _connectedDevices.Remove(deviceId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConnectedDeviceHeartbeatService is starting. Interval: {Interval}", _connectedDeviceConfig.HeartbeatInterval);
            while (!stoppingToken.IsCancellationRequested)
            {
                Guid[] connectedDevices = _connectedDevices.ToArray();
                _logger.LogDebug(message: "ConnectedDeviceHeartbeatService is running. Connected devices count: {Count}", connectedDevices.Length);
                IEnumerable<Task> tasks = connectedDevices.Select(deviceId =>
                {
                    try
                    {
                        return _connectedDeviceRepository.Heartbeat(deviceId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, message: "Error while update presence for device {DeviceId}", deviceId);
                        return Task.CompletedTask;
                    }
                });
                await Task.WhenAll(tasks);
                await Task.Delay(_connectedDeviceConfig.HeartbeatInterval, stoppingToken);
            }
        }
    }
}
