using CacheStorm.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CacheStorm.Services;

public class DbContextInMemoryService
{
    /// <summary>
    /// Add class or db sets with InMemoryAttribute into memory cache.
    /// </summary>
    /// <param name="serviceCollection"></param>
    public void AddEntitiesInToMemory(IServiceCollection serviceCollection)
    {
        var dbContextTypes = GetRegisteredDbContextTypes(serviceCollection);

        if (dbContextTypes is not null && dbContextTypes.Any())
        {
            foreach (var dbContext in dbContextTypes)
            {
                AddEntitiesInToMemory(dbContext, serviceCollection);
            }
        }
    }

    private ICollection<Type> GetRegisteredDbContextTypes(IServiceCollection serviceCollection)
    {
        var dbContexts = serviceCollection
            .Where(service =>
                service.ServiceType.IsSubclassOf(typeof(DbContext)) &&
                service.ServiceType.IsAbstract == false &&
                service.ServiceType.IsClass)
            .Select(service => service.ServiceType)
            .ToList();

        return dbContexts;
    }

    private void AddEntitiesInToMemory(Type dbContextType, IServiceCollection serviceCollection)
    {
        var dbSets = dbContextType
          .GetProperties()
          .Where(property =>
              property.PropertyType.IsGenericType &&
              property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        var inMemoryAttribute = dbContextType.GetCustomAttribute<InMemoryAttribute>();

        if (inMemoryAttribute is null)
        {
            dbSets = dbSets.Where(property => property.GetCustomAttribute<InMemoryAttribute>() is not null);
        }

        foreach (var dbSet in dbSets)
        {
            inMemoryAttribute = inMemoryAttribute ?? dbSet.GetCustomAttribute<InMemoryAttribute>();

            if (inMemoryAttribute is not null)
            {
                var entity = dbSet.PropertyType.GetGenericArguments().First();

                AddEntitiesInToMemory(entity, dbContextType, inMemoryAttribute, serviceCollection);
            }
        }
    }

    private void AddEntitiesInToMemory(Type entityType, Type dbContextType, InMemoryAttribute inMemoryAttribute, IServiceCollection serviceCollection)
    {
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var dbSet = CreateDbSet(serviceProvider, entityType, dbContextType);
        var entities = GetEntities(serviceProvider, entityType, dbSet);

        AddEntitiesInToMemory(serviceProvider, entities, inMemoryAttribute);
    }

    private object CreateDbSet(IServiceProvider serviceProvider, Type entityType, Type dbContextType)
    {
        var serviceDbContext = serviceProvider.GetService(dbContextType);
        var dbContextSetMethod = dbContextType.GetMethod(nameof(DbContext.Set), new[] { typeof(string) })!.MakeGenericMethod(entityType);
        var dbSet = dbContextSetMethod.Invoke(serviceDbContext, new object[] { entityType.FullName! });

        return dbSet!;
    }

    private object GetEntities(IServiceProvider serviceProvider, Type entityType, object dbSet)
    {
        var enumerableToListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(entityType);
        var entities = enumerableToListMethod.Invoke(null, new[] { dbSet });

        return entities!;
    }

    private void AddEntitiesInToMemory(IServiceProvider serviceProvider, object entities, InMemoryAttribute inMemoryAttribute)
    {
        var memoryCacheService = serviceProvider.GetService<IMemoryCache>();

        if (memoryCacheService is not null && memoryCacheService.Get(inMemoryAttribute.Key) is null)
        {
            memoryCacheService.Set(inMemoryAttribute.Key, entities, inMemoryAttribute.ExpirationPeriod);
        }
    }
}