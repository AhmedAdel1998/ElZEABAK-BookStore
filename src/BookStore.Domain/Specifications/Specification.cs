using System.Linq.Expressions;

namespace BookStore.Domain.Specifications;

/// <summary>
/// Base implementation of a domain specification.
/// </summary>
/// <typeparam name="T">The aggregate type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; protected init; }
}
