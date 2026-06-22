using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Linq;

namespace QuakePulse_WebService.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache key not found: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit: {Key}", key);
                return JsonSerializer.Deserialize<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache with key: {Key}", key);
                throw;
            }
        }

        public async Task SetAsync<T>(string key, T data, int ttlMinutes)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var expiry = TimeSpan.FromMinutes(ttlMinutes);

                await _db.StringSetAsync(key, json, expiry);
                _logger.LogDebug("Data cached successfully: {Key} (TTL: {TTL} minutes)", key, ttlMinutes);
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Error setting cache with key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Acquires a distributed lock using Redis SET NX (Set if Not eXists).
        /// This prevents cache stampede by ensuring only one request fetches data.
        /// </summary>
        public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, int lockDurationSeconds)
        {
            try
            {
                var result = await _db.StringSetAsync(
                    lockKey,
                    lockValue,
                    TimeSpan.FromSeconds(lockDurationSeconds),
                    When.NotExists
                );

                if (result)
                {
                    _logger.LogInformation("Lock acquired: {LockKey}", lockKey);
                }
                else
                {
                    _logger.LogDebug("Lock already held: {LockKey}", lockKey);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring lock: {LockKey}", lockKey);
                throw;
            }
        }

        /// <summary>
        /// Releases a distributed lock if the lock value matches (safe deletion).
        /// </summary>
        public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
        {
            try
            {
                const string releaseScript = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('del', KEYS[1])
else
    return 0
end";

                var result = await _db.ScriptEvaluateAsync(
                    releaseScript,
                    new RedisKey[] { lockKey },
                    new RedisValue[] { lockValue });

                var released = (long)result == 1;
                if (released)
                    _logger.LogInformation("Lock released: {LockKey}", lockKey);
                else
                    _logger.LogWarning("Lock value mismatch, could not release: {LockKey}", lockKey);
                return released;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing lock: {LockKey}", lockKey);
                throw;
            }
        }

        /// <summary>
        /// Gets the current lock value (for debugging/monitoring).
        /// </summary>
        public async Task<string?> GetLockAsync(string lockKey)
        {
            try
            {
                var value = await _db.StringGetAsync(lockKey);
                return value.IsNullOrEmpty ? null : value.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lock: {LockKey}", lockKey);
                return null;
            }
        }
    }
}
