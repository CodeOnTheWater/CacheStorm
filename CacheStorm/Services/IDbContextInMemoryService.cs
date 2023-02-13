namespace CacheStorm.Services
{
    public interface IDbContextInMemoryService
    {
        Task AddEntitiesToInMemory();
    }
}
