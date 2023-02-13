namespace CacheStorm.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class InMemoryAttribute : Attribute
    {
        public TimeSpan? ExpirationPeriod { get; set; }
        public InMemoryAttribute(TimeSpan expirationPeriod)
        {
            ExpirationPeriod = expirationPeriod;
        }

        public InMemoryAttribute()
        {
            ExpirationPeriod ??= TimeSpan.FromMinutes(30);
        }
    }
}
