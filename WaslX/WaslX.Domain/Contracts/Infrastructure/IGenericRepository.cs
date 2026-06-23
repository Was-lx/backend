using WaslX.Domain.Contracts.Specification;

namespace WaslX.Domain.Contracts.Infrastructure;

public interface IGenericRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    Task<IEnumerable<TEntity>> GetAllAsync(bool withTracking = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllWithSpecAsync(ISpecification<TEntity, TKey> specification, bool withTracking = false);
    Task<int> GetCountWithSpecAsync(ISpecification<TEntity, TKey> specification, bool withTracking = false);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetWithSpecAsync(ISpecification<TEntity, TKey> specification);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}