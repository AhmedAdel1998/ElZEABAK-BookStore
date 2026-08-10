using BookStore.Domain.Entities;
using BookStore.Domain.Specifications;

namespace BookStore.Domain.Interfaces;

/// <summary>
/// Defines role persistence operations.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Gets a role by identifier.
    /// </summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The role when found; otherwise, <see langword="null"/>.</returns>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching roles.</returns>
    Task<IReadOnlyCollection<Role>> ListAsync(ISpecification<Role>? specification = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a role.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
}
