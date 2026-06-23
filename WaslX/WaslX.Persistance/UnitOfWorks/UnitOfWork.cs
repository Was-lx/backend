using System.Collections.Concurrent;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Persistance.Data;
using WaslX.Persistance.Repos;

namespace WaslX.Persistance.UnitOfWorks;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    private readonly ConcurrentDictionary<string, object> _repositories = new();

    public async Task<int> CompleteAsync() => await dbContext.SaveChangesAsync();

    public ValueTask DisposeAsync() => dbContext.DisposeAsync();

    public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
        return (IGenericRepository<TEntity, TKey>)_repositories
            .GetOrAdd(typeof(TEntity).Name, new GenericRepository<TEntity, TKey>(dbContext));
    }
}
