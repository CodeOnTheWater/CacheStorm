using CacheStorm.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CacheStorm.Extensions;

public static class ServiceProviderExtension
{

    /// <summary>
    /// Add all or selected entities into memory cache.
    /// To add all entities into memory cache, InMemoryAttribute should be imposed on DbContext subclass.
    /// To add selected entities into memory cache, InMemoryAttribute should be imposed on particular DbSets.
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddEntitiesIntoMemoryCache(this IServiceProvider serviceProvider, ILogger logger = null)
    {
        try
        {
            AddEntitiesIntoMemoryCache(serviceProvider);
        }
        catch (Exception exception)
        {
            if (logger is not null)
            {
                logger.LogError(exception, "Exception was thrown during the process of adding entities into memory cache.");
            }
        }
    }

    private static void AddEntitiesIntoMemoryCache(IServiceProvider serviceProvider)
    {
        var dbContextTypes = GetDbContextTypes();

        if (dbContextTypes is null || dbContextTypes.Any() == false)
        {
            throw new ArgumentNullException("DB context does not exist.");
        }

        using var scope = serviceProvider.CreateScope();

        AddEntitiesIntoMemoryCacheByDbContextTypes(scope.ServiceProvider, dbContextTypes);
    }

    private static ICollection<Type> GetDbContextTypes()
    {
        var dbContextes = Assembly.GetEntryAssembly()!.GetTypes().Where(type =>
                type.IsSubclassOf(typeof(DbContext)) &&
                type.IsAbstract == false &&
                type.IsClass)
            .Select(type => type)
            .ToList();

        return dbContextes;
    }

    private static void AddEntitiesIntoMemoryCacheByDbContextTypes(IServiceProvider serviceProvider, ICollection<Type> dbContextTypes)
    {
        foreach (var dbContextType in dbContextTypes)
        {
            AddEntitiesIntoMemoryCacheByDbContextType(serviceProvider, dbContextType);
        }
    }

    private static ICollection<PropertyInfo> GetDbSetProperties(Type dbContextType)
    {
        var dbSets = dbContextType
          .GetProperties()
          .Where(property =>
              property.PropertyType.IsGenericType &&
              property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        dbSets = dbContextType.GetCustomAttribute<InMemoryAttribute>() is not null
            ? dbSets
            : dbSets.Where(property => property.GetCustomAttribute<InMemoryAttribute>() is not null);

        return dbSets.ToList();
    }

    private static void AddEntitiesIntoMemoryCacheByDbContextType(IServiceProvider serviceProvider, Type dbContextType)
    {
        var dbSetProperties = GetDbSetProperties(dbContextType);

        foreach (var dbSetProperty in dbSetProperties)
        {
            AddEntityIntoMemoryCacheByDbSetProperty(serviceProvider, dbSetProperty, dbContextType);
        }
    }

    private static void AddEntityIntoMemoryCacheByDbSetProperty(IServiceProvider serviceProvider, PropertyInfo dbSetProperty, Type dbContextType)
    {
        var inMemoryAttribute = dbContextType.GetCustomAttribute<InMemoryAttribute>() ?? dbSetProperty.GetCustomAttribute<InMemoryAttribute>();
        var entityType = dbSetProperty.PropertyType.GetGenericArguments().First();

        AddEntityIntoMemoryCache(serviceProvider, entityType, dbContextType, inMemoryAttribute!);
    }

    private static object CreateDbSet(IServiceProvider serviceProvider, Type entityType, Type dbContextType)
    {
        var dbContext = serviceProvider.GetService(dbContextType);
        var dbSetMethod = dbContextType.GetMethod(nameof(DbContext.Set), new[] { typeof(string) })!.MakeGenericMethod(entityType);
        var dbSet = dbSetMethod.Invoke(dbContext, new object[] { entityType.FullName! });

        return dbSet!;
    }

    private static object GetEntities(Type entityType, object dbSet)
    {
        var enumerableToListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(entityType);
        var entities = enumerableToListMethod.Invoke(null, new[] { dbSet });

        return entities!;
    }

    private static void AddEntityIntoMemoryCache(IServiceProvider serviceProvider, Type entityType, Type dbContextType, InMemoryAttribute inMemoryAttribute)
    {
        var scope = serviceProvider.CreateScope();
        var memoryCacheService = scope.ServiceProvider.GetService<IMemoryCache>();

        if (memoryCacheService is null)
        {
            throw new ArgumentNullException("MemoryCache is not registered.");
        }     

        if (memoryCacheService.Get(inMemoryAttribute.Key) is null)
        {
            var dbSet = CreateDbSet(scope.ServiceProvider, entityType, dbContextType);
            var entities = GetEntities(entityType, dbSet);

            memoryCacheService.Set(inMemoryAttribute.Key, entities, inMemoryAttribute.ExpirationPeriod);
        }
    }
}