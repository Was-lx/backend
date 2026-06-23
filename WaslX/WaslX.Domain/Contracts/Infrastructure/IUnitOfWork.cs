namespace WaslX.Domain.Contracts.Infrastructure;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> CompleteAsync();
    IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
        where TEntity : class
        where TKey : IEquatable<TKey>;
}