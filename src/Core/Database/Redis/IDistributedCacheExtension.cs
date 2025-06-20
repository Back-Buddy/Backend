using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BackBuddy.Core.Library.Database.Redis
{
    public static class IDistributedCacheExtension
    {
        public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default) where T : class?
        {
            byte[]? data = await cache.GetAsync(key, cancellationToken);
            if (data == null)
                return default;
            return JsonSerializer.Deserialize<T>(data, options);
        }

        public static async Task SetAsync<T>(this IDistributedCache cache, string key, T payload, JsonSerializerOptions? jsonOptions = null, DistributedCacheEntryOptions? distributedCacheEntryOptions = null, CancellationToken cancellationToken = default) where T : class
        {
            string payloadString = JsonSerializer.Serialize(payload, jsonOptions);
            if (distributedCacheEntryOptions != null)
                await cache.SetStringAsync(key, payloadString, distributedCacheEntryOptions, cancellationToken);
            else
                await cache.SetStringAsync(key, payloadString, cancellationToken);
        }
    }
}
