using System.Linq.Expressions;

namespace BookStore.Domain.Specifications;

/// <summary>
/// Describes query criteria for an aggregate.
/// </summary>
/// <typeparam name="T">The aggregate type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the filtering criteria.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }
}
