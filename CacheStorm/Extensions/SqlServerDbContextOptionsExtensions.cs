using CacheStorm.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CacheStorm.Extensions
{
    public static class SqlServerDbContextOptionsExtensions
    {
        public static async Task AddDbContextInMemory(this IServiceCollection serviceCollection, IServiceProvider serviceProvider)
        {
            serviceCollection.AddScoped<IDbContextInMemoryService, DbContextInMemoryService>();

            var dbContextInMemoryService = serviceProvider.GetService<IDbContextInMemoryService>();

            if (dbContextInMemoryService is not null)
            {
                await dbContextInMemoryService.AddEntitiesToInMemory();
            }
        }
    }
}
