namespace QuakePulse_WebService.Caching
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T data, int ttlMinutes);
        Task<bool> AcquireLockAsync(string lockKey, string lockValue, int lockDurationSeconds);
        Task<bool> ReleaseLockAsync(string lockKey, string lockValue);
        Task<string?> GetLockAsync(string lockKey);
    }
}
