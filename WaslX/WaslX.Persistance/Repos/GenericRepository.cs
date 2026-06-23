using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Contracts.Specification;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Repos;

public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
where TEntity : class
where TKey : IEquatable<TKey>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = _dbContext.Set<TEntity>();
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(bool withTracking = false, CancellationToken cancellationToken = default)
    {
        return withTracking
            ? await _dbSet.ToListAsync(cancellationToken)
            : await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllWithSpecAsync(ISpecification<TEntity, TKey> specification, bool withTracking = false)
    {
        var query = withTracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
        return await SpecificationEvaluator<TEntity, TKey>.GetQuery(query, specification).ToListAsync();
    }

    public async Task<int> GetCountWithSpecAsync(ISpecification<TEntity, TKey> specification, bool withTracking = false)
    {
        var query = withTracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
        return await SpecificationEvaluator<TEntity, TKey>.GetQuery(query, specification).CountAsync();
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public async Task<TEntity?> GetWithSpecAsync(ISpecification<TEntity, TKey> specification)
        => await SpecificationEvaluator<TEntity, TKey>.GetQuery(_dbSet.AsQueryable(), specification).FirstOrDefaultAsync();

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity)
        => _dbSet.Update(entity);

    public void Delete(TEntity entity)
        => _dbSet.Remove(entity);
}
