namespace CacheStorm.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class InMemoryAttribute : Attribute
{
    public TimeSpan ExpirationPeriod { get; set; } = TimeSpan.FromMinutes(30);
    public InMemoryAttribute(TimeSpan expirationPeriod)
    {
        ExpirationPeriod = expirationPeriod;
    }

    public InMemoryAttribute()
    {

    }
}