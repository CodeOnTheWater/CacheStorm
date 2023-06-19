namespace CacheStorm.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class InMemoryAttribute : Attribute
{
    public TimeSpan ExpirationPeriod { get; } = TimeSpan.FromSeconds(30 * 60);
    public string Key { get; }

    public InMemoryAttribute(string key, int expirationPeriodInSeconds)
    {
        Key = key;
        ExpirationPeriod = TimeSpan.FromSeconds(expirationPeriodInSeconds);
    }

    public InMemoryAttribute(string key)
    {
        Key = key;
    }
}