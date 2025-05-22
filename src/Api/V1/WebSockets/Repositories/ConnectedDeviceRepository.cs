using BackBuddy.Api.Service.V1.Database.Redis;
using BackBuddy.Api.Service.V1.WebSockets.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace BackBuddy.Api.Service.V1.WebSockets.Repositories
{
    public interface IConnectedDeviceRepository
    {
        Task Add(Guid deviceId, ConnectedDevice deviceConnected, CancellationToken cancellationToken = default);
        Task<ConnectedDevice?> Get(Guid deviceId, CancellationToken cancellationToken = default);
        Task Remove(Guid deviceId, CancellationToken cancellationToken = default);
    }

    public class ConnectedDeviceRepository(IDistributedCache distributedCache) : IConnectedDeviceRepository
    {
        private readonly IDistributedCache _distributedCache = distributedCache;

        public async Task Add(Guid deviceId, ConnectedDevice deviceConnected, CancellationToken cancellationToken = default)
        {
            await _distributedCache.SetAsync(GetCacheKey(deviceId), deviceConnected, cancellationToken: cancellationToken);
        }

        public async Task<ConnectedDevice?> Get(Guid deviceId, CancellationToken cancellationToken = default)
        {
            ConnectedDevice? deviceConnected = await _distributedCache.GetAsync<ConnectedDevice?>(GetCacheKey(deviceId), cancellationToken: cancellationToken);
            return deviceConnected;
        }

        public async Task Remove(Guid deviceId, CancellationToken cancellationToken = default)
        {
            await _distributedCache.RemoveAsync(GetCacheKey(deviceId), cancellationToken);
        }

        private static string GetCacheKey(Guid deviceId) => $"DeviceConnected:{deviceId}";
    }
}
