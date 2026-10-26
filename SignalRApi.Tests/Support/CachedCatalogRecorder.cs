using SignalRWebUI.Services;

namespace SignalR.Api.Tests.Support
{
    /// <summary>
    /// Records Redis-backed cache reads/writes so tests can verify that the app
    /// doesn't bypass the cache when a record is already present.
    /// </summary>
    public class CachedCatalogRecorder : ICatalogCache
    {
        private readonly Dictionary<string, object> Cache = new();
        private readonly List<string> Writes = new();
        private readonly List<string> ReadKeys = new();
        private readonly List<string> Removed = new();

        public Task<T?> GetListAsync<T>(string cacheKey) where T : class
        {
            ReadKeys.Add(cacheKey);
            var cached = Cache.TryGetValue(cacheKey, out var value) && value is T typed
                ? typed
                : null;
            return Task.FromResult<T?>(cached);
        }

        public Task SetListAsync<T>(string cacheKey, T value,
            TimeSpan? absoluteExpirationRelativeToNow = null)
            where T : class
        {
            Cache[cacheKey] = value;
            Writes.Add(cacheKey);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string cacheKey)
        {
            Cache.Remove(cacheKey);
            Removed.Add(cacheKey);
            return Task.CompletedTask;
        }

        public async Task<bool> WasCacheEntryCreatedAsync(string cacheKey)
        {
            return Cache.ContainsKey(cacheKey);
        }

        public IReadOnlyCollection<string> Keys => Cache.Keys.ToArray();
        public IReadOnlyCollection<string> WriteLog => Writes.ToArray();
        public IReadOnlyCollection<string> ReadLog => ReadKeys.ToArray();

        public async Task<bool> WasRemoved(string key)
        {
            return await Task.FromResult(Removed.Contains(key));
        }
    }
}
