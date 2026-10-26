using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace SignalRWebUI.Services
{
    /// <summary>
    /// Caches server-rendered operation payloads in Redis. This keeps peak traffic off
    /// the API/database while leaving the pages themselves server-rendered.
    /// </summary>
    public interface ICatalogCache
    {
        Task<T?> GetListAsync<T>(string cacheKey) where T : class;

        Task SetListAsync<T>(string cacheKey, T value, TimeSpan? absoluteExpirationRelativeToNow = null)
            where T : class;

        Task RemoveAsync(string cacheKey);

        Task<bool> WasCacheEntryCreatedAsync(string cacheKey);
    }

    public class RedisCatalogCache : ICatalogCache
    {
        private readonly IDistributedCache _cache;

        public RedisCatalogCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetListAsync<T>(string cacheKey) where T : class
        {
            var bytes = await _cache.GetAsync(cacheKey);
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(bytes);
        }

        public async Task SetListAsync<T>(string cacheKey, T value, TimeSpan? absoluteExpirationRelativeToNow = null)
            where T : class
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(2)
            };
            await _cache.SetAsync(cacheKey, bytes, options);
        }

        public Task RemoveAsync(string cacheKey)
        {
            return _cache.RemoveAsync(cacheKey);
        }

        public async Task<bool> WasCacheEntryCreatedAsync(string cacheKey)
        {
            var bytes = await _cache.GetAsync(cacheKey);
            return bytes is not null && bytes.Length > 0;
        }
    }
}
