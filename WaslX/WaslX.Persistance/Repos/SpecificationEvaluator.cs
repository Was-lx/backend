using Microsoft.EntityFrameworkCore;
using WaslX.Domain.Contracts.Specification;

namespace WaslX.Persistance.Repos;

internal static class SpecificationEvaluator<TEntity, TKey>
where TEntity : class
where TKey : IEquatable<TKey>
{
    public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> query, ISpecification<TEntity, TKey> specification)
    {
        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        if (specification.OrderByDesc is not null)
            query = query.OrderByDescending(specification.OrderByDesc);
        else if (specification.OrderBy is not null)
            query = query.OrderBy(specification.OrderBy);

        if (specification.Includes is not null)
            query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.IsGettingTopQuery)
            query = query.Take(specification.Take);
        else if (specification.IsPaginationEnabled)
            query = query.Skip(specification.Skip).Take(specification.Take);

        return query;
    }
}
