using CacheStorm.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CacheStorm.Extensions;

public static class ServiceCollectionExtension
{
    public static async Task AddDbContextToInMemoryCache(this IServiceCollection serviceCollection)
    {
        try
        {
            serviceCollection.AddMemoryCache();
            serviceCollection.AddScoped<IDbContextInMemoryService, DbContextInMemoryService>();

            using var serviceProvider = serviceCollection.BuildServiceProvider();
            
            var dbContextInMemoryService = serviceProvider.GetService<IDbContextInMemoryService>();

            dbContextInMemoryService.AddEntitiesInToMemoryCache(serviceCollection);
        }
        catch (Exception exception)
        {

        }
    }
}