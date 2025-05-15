using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;

namespace eRaptors.Services
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expirationTime = null);
        void Remove(string key);
        T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expirationTime = null);
        void ClearAll();
        IEnumerable<string> GetAllKeys();
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly MemoryCacheEntryOptions _defaultOptions;
        private static readonly HashSet<string> _cacheKeys = new();

        public MemoryCacheService(
            IMemoryCache cache,
            ILogger<MemoryCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));
        }

        public T? Get<T>(string key)
        {
            return _cache.Get<T>(key);
        }

        public void Set<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var options = expirationTime.HasValue
                ? new MemoryCacheEntryOptions().SetAbsoluteExpiration(expirationTime.Value)
                : _defaultOptions;

            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                var keyStr = key.ToString();
                if (keyStr != null)
                {
                    _cacheKeys.Remove(keyStr);
                    _logger.LogDebug("Removed key from tracking: {Key}", keyStr);
                }
            });

            _cache.Set(key, value, options);
            _cacheKeys.Add(key);
            _logger.LogDebug("Added key to tracking: {Key}", key);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _cacheKeys.Remove(key);
            _logger.LogInformation("Removed from cache: {Key}", key);
        }

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expirationTime = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            return _cache.GetOrCreate(key, entry =>
            {
                var options = expirationTime.HasValue
                    ? new MemoryCacheEntryOptions().SetAbsoluteExpiration(expirationTime.Value)
                    : _defaultOptions;

                entry.SetOptions(options);

                var result = factory();
                if (result != null)
                {
                    _cacheKeys.Add(key);
                    _logger.LogDebug("Added key to tracking: {Key}", key);
                }

                return result;
            });
        }

        public void ClearAll()
        {
            try
            {
                foreach (var key in _cacheKeys.ToList())
                {
                    _cache.Remove(key);
                }
                _cacheKeys.Clear();
                _logger.LogInformation("Cache cleared successfully. Total keys cleared: {Count}", _cacheKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                throw;
            }
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _cacheKeys.ToList();
        }
    }
}
