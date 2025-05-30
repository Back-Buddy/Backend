using BackBuddy.Api.Service.V1.Database.Redis;
using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace BackBuddy.Api.Service.V1.WebSockets.Repositories
{
    public interface IConnectedDeviceRepository
    {
        Task Add(Guid deviceId, ConnectedDevice deviceConnected, CancellationToken cancellationToken = default);
        Task Heartbeat(Guid deviceId, CancellationToken cancellationToken = default);
        Task<ConnectedDevice?> Get(Guid deviceId, CancellationToken cancellationToken = default);
        Task<bool> IsConnected(Guid deviceId, CancellationToken cancellationToken = default);
        Task Remove(Guid deviceId, CancellationToken cancellationToken = default);
    }

    public class ConnectedDeviceRepository(IOptions<ConnectedDeviceConfig> options, IDistributedCache distributedCache) : IConnectedDeviceRepository
    {
        private readonly ConnectedDeviceConfig _connectedDeviceConfig = options.Value;
        private readonly IDistributedCache _distributedCache = distributedCache;

        public async Task Add(Guid deviceId, ConnectedDevice deviceConnected, CancellationToken cancellationToken = default)
        {
            DistributedCacheEntryOptions distributedCacheEntryOptions = new()
            {
                AbsoluteExpirationRelativeToNow = _connectedDeviceConfig.MetaTimeout
            };
            await _distributedCache.SetAsync(GetCacheKeyMeta(deviceId), deviceConnected, distributedCacheEntryOptions: distributedCacheEntryOptions, cancellationToken: cancellationToken);
            await Heartbeat(deviceId, cancellationToken);
        }

        public async Task Heartbeat(Guid deviceId, CancellationToken cancellationToken = default)
        {
            DistributedCacheEntryOptions distributedCacheEntryOptions = new()
            {
                AbsoluteExpirationRelativeToNow = _connectedDeviceConfig.PresenceTimeout
            };
            await _distributedCache.SetStringAsync(GetCacheKeyPresence(deviceId), "Online", options: distributedCacheEntryOptions, token: cancellationToken);
        }

        public async Task<ConnectedDevice?> Get(Guid deviceId, CancellationToken cancellationToken = default)
        {
            ConnectedDevice? deviceConnected = await _distributedCache.GetAsync<ConnectedDevice?>(GetCacheKeyMeta(deviceId), cancellationToken: cancellationToken);
            return deviceConnected;
        }

        public async Task<bool> IsConnected(Guid deviceId, CancellationToken cancellationToken = default)
        {
            string? deviceConnected = await _distributedCache.GetStringAsync(GetCacheKeyPresence(deviceId), token: cancellationToken);
            return !string.IsNullOrEmpty(deviceConnected);
        }

        public async Task Remove(Guid deviceId, CancellationToken cancellationToken = default)
        {
            await _distributedCache.RemoveAsync(GetCacheKeyMeta(deviceId), cancellationToken);
            await _distributedCache.RemoveAsync(GetCacheKeyPresence(deviceId), cancellationToken);
        }

        private static string GetCacheKeyPresence(Guid deviceId) => $"DeviceConnected:{deviceId}:presence";
        private static string GetCacheKeyMeta(Guid deviceId) => $"DeviceConnected:{deviceId}:meta";
    }
}
