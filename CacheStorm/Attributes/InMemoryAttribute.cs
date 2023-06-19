namespace CacheStorm.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class InMemoryAttribute : Attribute
{
    public TimeSpan ExpirationPeriod { get; } = TimeSpan.FromMinutes(30);
    public string Key { get; }

    public InMemoryAttribute(string key,TimeSpan expirationPeriod)
    {
        Key = key;
        ExpirationPeriod = expirationPeriod;
    }

    public InMemoryAttribute(string key)
    {
        Key = key;
    }
}