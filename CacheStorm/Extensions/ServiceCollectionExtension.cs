using CacheStorm.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CacheStorm.Extensions;

public static class ServiceCollectionExtension
{
    public static void AddDbContextToInMemoryCache(this IServiceCollection serviceCollection)
    {
        try
        {
            var dbContextInMemoryService = new DbContextInMemoryService();

            dbContextInMemoryService.AddEntitiesInToMemory(serviceCollection);
        }
        catch
        {
            //swallow
        }
    }
}