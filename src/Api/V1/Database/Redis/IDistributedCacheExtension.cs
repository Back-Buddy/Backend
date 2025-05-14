using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.Database.Redis
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

        public static async Task SetAsync<T>(this IDistributedCache cache, string key, T payload, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            string payloadString = JsonSerializer.Serialize<T>(payload, options);
            await cache.SetStringAsync(key, payloadString, cancellationToken);
        }
    }
}
