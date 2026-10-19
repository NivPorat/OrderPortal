using OrderPortal.ApiService.Models;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading;
namespace OrderPortal.ApiService.Services
{
    public class CachingService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<CachingService> _logger;
        public CachingService(IConnectionMultiplexer redis, ILogger<CachingService> logger)
        {
            _redisDb = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<Product?> GetCachedKeyAsync(string id)
        {
            string cacheKey = $"product:{id}";

            string? cachedProductJson = await _redisDb.StringGetAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedProductJson))
            {
                _logger.LogInformation("Cache hit for product ID: {Id}", id);
                return JsonSerializer.Deserialize<Product>(cachedProductJson);
            }
            _logger.LogInformation("No cache data was found for ID: {Id}, Resorting to check DB", id);
            return null;
        }

        public async Task SetCachedKeyAsync(string id, Product product)
        {
            string cacheKey = $"product:{id}";
            string serializedProduct = JsonSerializer.Serialize(product);

            await _redisDb.StringSetAsync(cacheKey, serializedProduct, TimeSpan.FromHours(24));
        }
        public async Task InvalidateKeyAsync(string id)
        {
            string cacheKey = $"product:{id}";

            // Delete the key from Redis
            await _redisDb.KeyDeleteAsync(cacheKey);

            _logger.LogInformation("Cache invalidated (deleted) for product ID: {Id}", id);
        }
    }
}
