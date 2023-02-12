using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CacheStorm.Services
{
    public class DbContextDecorator
    {
        private readonly DbContext _dbContext;
        private readonly IMemoryCache _memoryCache;

        public DbContextDecorator(DbContext dbContext, IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _memoryCache = memoryCache;
        }

        public async Task GetAll()
        {

        }

        public async Task GetSelected(ICollection<Type> dbSets)
        {
            foreach (var dbSet in dbSets)
            {
                await GetSelected(dbSet);
            }
        }

        private async Task GetSelected<TEntity>(TEntity entity) where TEntity : class
        {
            var dbContextSetter = _dbContext.Set<TEntity>();
            var dbSet = await dbContextSetter.ToListAsync();

            //Add some validation.
            _memoryCache.Set(entity, dbSet);
        }
    }
}
