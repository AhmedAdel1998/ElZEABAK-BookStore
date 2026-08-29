using BookStore.Domain.Entities;
using BookStore.Domain.Specifications;

namespace BookStore.Domain.Interfaces;

/// <summary>
/// Defines user persistence operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The user when found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The user when found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching users.</returns>
    Task<IReadOnlyCollection<User>> ListAsync(ISpecification<User>? specification = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user.
    /// </summary>
    /// <param name="user">The user to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a username is already assigned.</summary>
    Task<bool> ExistsByUsernameAsync(string username, Guid? excludedUserId = null, CancellationToken cancellationToken = default);
}
