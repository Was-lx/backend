using System.Linq.Expressions;

namespace WaslX.Domain.Contracts.Specification;

public interface ISpecification<TEntity, TKey>
    where TKey : IEquatable<TKey>
{
    Expression<Func<TEntity, bool>>? Criteria { get; set; }
    List<Expression<Func<TEntity, object>>> Includes { get; set; }
    Expression<Func<TEntity, object>>? OrderBy { get; set; }
    Expression<Func<TEntity, object>>? OrderByDesc { get; set; }
    int Take { get; set; }
    int Skip { get; set; }
    bool IsPaginationEnabled { get; set; }
    bool IsGettingTopQuery { get; set; }
}
