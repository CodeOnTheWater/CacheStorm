using Microsoft.Extensions.DependencyInjection;

namespace CacheStorm.Services;

public interface IDbContextInMemoryService
{
    void AddEntitiesInToMemoryCache(IServiceCollection serviceCollection);
}