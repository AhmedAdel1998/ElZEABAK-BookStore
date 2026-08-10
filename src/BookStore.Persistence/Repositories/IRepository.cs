using BookStore.Domain.Common;
using BookStore.Domain.Specifications;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Defines common repository operations for aggregate roots.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    /// Gets an entity by identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The entity when found; otherwise, <see langword="null"/>.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities matching an optional specification.
    /// </summary>
    /// <param name="specification">The optional specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching entities.</returns>
    Task<IReadOnlyCollection<TEntity>> ListAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an entity.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}
