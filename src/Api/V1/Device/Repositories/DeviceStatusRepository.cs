using BackBuddy.Api.Service.V1.Database.Redis;
using BackBuddy.Api.Service.V1.Device.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public interface IDeviceStatusRepository
    {
        Task<DeviceStatusEntity?> GetCurrentStatus(Guid deviceId, CancellationToken cancellationToken = default);
        Task SetCurrentStatus(Guid deviceId, DeviceStatusEntity deviceStatus, CancellationToken cancellationToken = default);
        Task DeleteCurrentStatus(Guid deviceId, CancellationToken cancellationToken = default);
    }

    public class DeviceStatusRepository(IDistributedCache cache) : IDeviceStatusRepository
    {
        private readonly static JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IDistributedCache _cache = cache;

        public async Task<DeviceStatusEntity?> GetCurrentStatus(Guid deviceId, CancellationToken cancellationToken = default)
        {
            return await _cache.GetAsync<DeviceStatusEntity?>(GetCacheKey(deviceId), _jsonOptions, cancellationToken);
        }

        public async Task SetCurrentStatus(Guid deviceId, DeviceStatusEntity deviceStatus, CancellationToken cancellationToken = default)
        {
            await _cache.SetAsync(GetCacheKey(deviceId), deviceStatus, jsonOptions: _jsonOptions, cancellationToken: cancellationToken);
        }

        public async Task DeleteCurrentStatus(Guid deviceId, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetCacheKey(deviceId), cancellationToken);
        }

        private static string GetCacheKey(Guid deviceId) => $"DeviceStatus:{deviceId}";

    }
}
