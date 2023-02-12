using CacheStorm.Services;
using CacheStorm.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace CacheStorm.Registries
{
    public static class DbContextDecoratorRegistry
    {
        public static void RegisterDbContextDecorator(
            this IServiceCollection serviceCollection, 
            IServiceProvider serviceProvider, 
            DbContextSelectionType dbContextSelectionType, 
            params Type[] dbSets)
        {
            serviceCollection.AddTransient<DbContextDecorator>();
 
            var service = serviceProvider.GetService(typeof(DbContextDecorator));

            switch (dbContextSelectionType)
            {
                case DbContextSelectionType.All:
           
                    break;
                case DbContextSelectionType.Selected:

                    break;
                case DbContextSelectionType.None:
                    break;
            }            
        }
    }
}
