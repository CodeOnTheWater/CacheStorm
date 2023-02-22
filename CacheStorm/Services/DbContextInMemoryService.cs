using CacheStorm.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CacheStorm.Services;

public class DbContextInMemoryService : IDbContextInMemoryService
{
    public void AddEntitiesInToMemoryCache(IServiceCollection serviceCollection)
    {
        var dbContextSuccessors = GetRegisteredDbContextSuccessors(serviceCollection);

        if (dbContextSuccessors is not null)
        {
            foreach (var dbContextSuccessor in dbContextSuccessors)
            {
                AddEntitiesInToMemoryCacheForDbContextSuccessor(dbContextSuccessor, serviceCollection);
            }
        }
    }

    private ICollection<Type> GetRegisteredDbContextSuccessors(IServiceCollection serviceCollection)
    {
        var dbContextSuccessors = serviceCollection
            .Where(service =>
                service.ServiceType.IsSubclassOf(typeof(DbContext)) &&
                service.ServiceType.IsAbstract == false &&
                service.ServiceType.IsClass)
            .Select(service => service.ServiceType)
            .ToList();

        return dbContextSuccessors;
    }

    private void AddEntitiesInToMemoryCacheForDbContextSuccessor(Type dbContextSuccessor, IServiceCollection serviceCollection)
    {
        var dbContextSuccessorWithInMemoryAttribute = dbContextSuccessor.GetCustomAttribute<InMemoryAttribute>();

        var dbSets = dbContextSuccessor
          .GetProperties()
          .Where(property =>
              property.PropertyType.IsGenericType &&
              property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        if (dbContextSuccessorWithInMemoryAttribute is null)
        {
            dbSets = dbSets.Where(property => property.GetCustomAttribute<InMemoryAttribute>() is not null);
        }

        foreach (var dbSet in dbSets.ToList())
        {
            var expirationPeriod = dbContextSuccessorWithInMemoryAttribute is not null
                ? dbContextSuccessorWithInMemoryAttribute.ExpirationPeriod
                : dbSet.GetCustomAttribute<InMemoryAttribute>()!.ExpirationPeriod;

            var entityType = dbSet.PropertyType.GetGenericArguments().First();

            AddToInMemory(entityType, dbContextSuccessor, serviceCollection, expirationPeriod);
        }
    }

    private void AddToInMemory(Type entity, Type dbContextSuccessor, IServiceCollection serviceCollection, TimeSpan expirationTime)
    {
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var serviceDbContext = serviceProvider.GetService(dbContextSuccessor);
        var dbContextSetMethod = dbContextSuccessor.GetMethod(nameof(DbContext.Set), new[] { typeof(string) })!.MakeGenericMethod(entity);
        var dbSet = dbContextSetMethod.Invoke(serviceDbContext, new object[] { entity.FullName! });

        var enumerableToListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(entity);
        var entities = enumerableToListMethod.Invoke(null, new[] { dbSet });

        var memoryCacheService = serviceProvider.GetService<IMemoryCache>();

        memoryCacheService.Set(entity.Name, entities, expirationTime);
    }
}