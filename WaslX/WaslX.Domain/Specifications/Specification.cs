using System.Linq.Expressions;
using WaslX.Domain.Contracts.Specification;

namespace WaslX.Domain.Specifications;

public class Specification<TEntity, TKey> : ISpecification<TEntity, TKey>
where TKey : IEquatable<TKey>
{
    public Expression<Func<TEntity, bool>>? Criteria { get; set; } = null;
    public List<Expression<Func<TEntity, object>>> Includes { get; set; } = [];
    public Expression<Func<TEntity, object>>? OrderBy { get; set; } = null;
    public Expression<Func<TEntity, object>>? OrderByDesc { get; set; } = null;
    public int Take { get; set; }
    public int Skip { get; set; }
    public bool IsPaginationEnabled { get; set; }
    public bool IsGettingTopQuery { get; set; }

    protected Specification() { }

    protected Specification(Expression<Func<TEntity, bool>> expression)
    {
        Criteria = expression;
    }

    protected void AddInclude(Expression<Func<TEntity, object>> include)
        => Includes.Add(include);

    protected void AddOrderBy(Expression<Func<TEntity, object>> orderBy)
        => OrderBy = orderBy;

    protected void AddOrderByDesc(Expression<Func<TEntity, object>> orderByDesc)
        => OrderByDesc = orderByDesc;

    protected void ApplyPagination(int skip, int take)
    {
        IsPaginationEnabled = true;
        Skip = skip;
        Take = take;
    }
}
