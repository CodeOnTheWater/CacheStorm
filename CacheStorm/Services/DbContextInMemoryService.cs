using CacheStorm.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

namespace CacheStorm.Services
{
    public class DbContextInMemoryService : IDbContextInMemoryService
    {
        private readonly DbContext _dbContext;
        private readonly IMemoryCache _memoryCache;

        public DbContextInMemoryService(DbContext dbContext, IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _memoryCache = memoryCache;
        }

        public async Task AddEntitiesToInMemory()
        {
            var dbContextSuccessors = GetDbContextSuccessors();

            foreach (var dbContextSuccessor in dbContextSuccessors)
            {
                var dbContextSuccessorWithInMemoryAttribute = dbContextSuccessor.GetCustomAttribute<InMemoryAttribute>();

                if (dbContextSuccessorWithInMemoryAttribute is not null)
                {
                    await AddDbContextSuccessorAllDbSetsToInMemory(dbContextSuccessor, dbContextSuccessorWithInMemoryAttribute.ExpirationPeriod!.Value);

                    continue;
                }

                var dbContextSucessorPropertiesWithInMemoryAttributes = dbContextSuccessor.GetProperties().Where(property => property.GetCustomAttribute<InMemoryAttribute>() is not null).ToList();

                if (dbContextSucessorPropertiesWithInMemoryAttributes is not null && dbContextSucessorPropertiesWithInMemoryAttributes.Any())
                {
                    await AddDbContextSuccessorSelectedDbSetsToInMemory(dbContextSucessorPropertiesWithInMemoryAttributes);
                }
            }
        }

        private async Task AddDbContextSuccessorAllDbSetsToInMemory(Type dbContextSuccessor, TimeSpan expirationTime)
        {
            var dbContextSuccessorProperties = dbContextSuccessor.GetProperties();
            var entities = dbContextSuccessorProperties.Where(dbContextSuccessorProperty =>
                dbContextSuccessorProperty.PropertyType.IsGenericType &&
                dbContextSuccessorProperty.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)).ToList();

            foreach (var entity in entities)
            {
                await AddToInMemory(entity, expirationTime);
            }
        }

        private async Task AddDbContextSuccessorSelectedDbSetsToInMemory(ICollection<PropertyInfo> dbContextSuccessorProperties)
        {
            foreach (var dbContextSuccessorProperty in dbContextSuccessorProperties)
            {
                var dbContextSuccessorPropertiesInMemoryAttribute = dbContextSuccessorProperty.GetCustomAttribute<InMemoryAttribute>();

                await AddToInMemory(dbContextSuccessorProperty.PropertyType, dbContextSuccessorPropertiesInMemoryAttribute!.ExpirationPeriod!.Value);
            }
        }

        private async Task AddToInMemory<TEntity>(TEntity entity, TimeSpan expirationTime) where TEntity : class
        {
            var dbContextSetter = _dbContext.Set<TEntity>();
            var dbSet = await dbContextSetter.ToListAsync();

            _memoryCache.Set(entity, dbSet, expirationTime);
        }

        private ICollection<Type> GetDbContextSuccessors()
        {
            var dbContextSuccessor = Assembly.GetExecutingAssembly()
                .GetTypes().Where(assembly =>
                    assembly.IsClass &&
                    assembly.IsAbstract == false &&
                    assembly.IsSubclassOf(typeof(DbContext)))
                .ToList();

            return dbContextSuccessor;
        }
    }
}
