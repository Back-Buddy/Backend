using BackBuddy.Api.Service.V1.Database.Redis;
using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Mapper;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public interface IDeviceStatusRepository
    {
        Task<DeviceStatusEntity?> GetCurrentStatus(Guid deviceId, CancellationToken cancellationToken = default);
        Task SetCurrentStatus(Guid deviceId, DeviceStatusEntity deviceStatus, CancellationToken cancellationToken = default);
        Task DeleteCurrentStatus(Guid deviceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DeviceStatusDto>> GetAllStatuses(CancellationToken cancellationToken = default);
        Task<DateTime?> GetLastNotificationTime(Guid deviceId, CancellationToken cancellationToken = default);
        Task SetLastNotificationTime(Guid deviceId, DateTime lastNotificationTime, CancellationToken cancellationToken = default);
    }

    public class DeviceStatusRepository(IDistributedCache cache, IConnectionMultiplexer connectionMultiplexer) : IDeviceStatusRepository
    {
        private const string CacheKeyPrefix = "DeviceStatus:";
        private const string MemberPrefix = "device_status_keys";

        private readonly static JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IDistributedCache _cache = cache;
        private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

        public async Task<DeviceStatusEntity?> GetCurrentStatus(Guid deviceId, CancellationToken cancellationToken = default)
        {
            return await _cache.GetAsync<DeviceStatusEntity?>(GetCacheKey(deviceId), _jsonOptions, cancellationToken);
        }

        public async Task SetCurrentStatus(Guid deviceId, DeviceStatusEntity deviceStatus, CancellationToken cancellationToken = default)
        {
            await _cache.SetAsync(GetCacheKey(deviceId), deviceStatus, jsonOptions: _jsonOptions, cancellationToken: cancellationToken);
            await _database.SetAddAsync(MemberPrefix, deviceId.ToString());
        }

        public async Task DeleteCurrentStatus(Guid deviceId, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetCacheKey(deviceId), cancellationToken);
            await _cache.RemoveAsync(GetLastNotificationKey(deviceId), cancellationToken);
            await _database.SetRemoveAsync(MemberPrefix, deviceId.ToString());
        }

        public async Task<IEnumerable<DeviceStatusDto>> GetAllStatuses(CancellationToken cancellationToken = default)
        {
            RedisValue[] keys = await _database.SetMembersAsync(MemberPrefix).ConfigureAwait(false);

            IEnumerable<Task<DeviceStatusDto?>> tasks = keys.Select(key => key.ToString())
                .Select(Guid.Parse)
                .Select(key => HandleGetStatus(key, cancellationToken));

            DeviceStatusDto?[] deviceStatusEntities = await Task.WhenAll(tasks);

            return deviceStatusEntities.Where(status => status != null)!;
        }

        private async Task<DeviceStatusDto?> HandleGetStatus(Guid deviceId, CancellationToken cancellationToken = default)
        {
            DeviceStatusEntity? status = await GetCurrentStatus(deviceId, cancellationToken).ConfigureAwait(false);
            if (status != null)
            {
                return status.ToDto(deviceId);
            }

            // If the status is null, we remove the key from the Redis set
            await _database.SetRemoveAsync(MemberPrefix, deviceId.ToString());
            return null;
        }

        public async Task<DateTime?> GetLastNotificationTime(Guid deviceId, CancellationToken cancellationToken = default)
        {
            string? lastNotificationTime = await _cache.GetStringAsync(GetLastNotificationKey(deviceId), cancellationToken);
            if (lastNotificationTime == null)
                return null;
            if (!long.TryParse(lastNotificationTime, out long unixTimeSeconds))
                return null;
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).UtcDateTime;
        }

        public async Task SetLastNotificationTime(Guid deviceId, DateTime lastNotificationTime, CancellationToken cancellationToken = default)
        {
            DateTimeOffset dateTimeOffset = (DateTimeOffset)lastNotificationTime.ToUniversalTime();
            await _cache.SetStringAsync(GetLastNotificationKey(deviceId), dateTimeOffset.ToUnixTimeSeconds().ToString(), token: cancellationToken);
        }

        private static string GetCacheKey(Guid deviceId) => $"{CacheKeyPrefix}{deviceId}";
        private static string GetLastNotificationKey(Guid deviceId) => $"{CacheKeyPrefix}{deviceId}:LastNotification";
    }
}
