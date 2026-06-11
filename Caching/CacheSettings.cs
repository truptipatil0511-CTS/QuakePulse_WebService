namespace QuakePulse_WebService.Caching
{
    public class CacheSettings
    {

        public bool Enabled { get; set; }
        public int LockDurationSeconds { get; set; }
        public int LockWaitTimeMs { get; set; }
        public int MaxLockWaitAttempts { get; set; }

    }
}
