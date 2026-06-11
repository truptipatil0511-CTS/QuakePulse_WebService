namespace QuakePulse_WebService.Caching
{
    // No-op cache service registered when CacheSettings:Enabled is false.
    // Keeps DI valid without requiring conditional constructor injection.
    public class NullCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key) => Task.FromResult(default(T?));
        public Task SetAsync<T>(string key, T data, int ttlMinutes) => Task.CompletedTask;
        public Task<bool> AcquireLockAsync(string lockKey, string lockValue, int lockDurationSeconds) => Task.FromResult(true);
        public Task<bool> ReleaseLockAsync(string lockKey, string lockValue) => Task.FromResult(true);
        public Task<string?> GetLockAsync(string lockKey) => Task.FromResult<string?>(null);
    }
}
