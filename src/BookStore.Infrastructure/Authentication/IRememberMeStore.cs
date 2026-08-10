namespace BookStore.Infrastructure.Authentication;

/// <summary>
/// Stores encrypted remember-me tokens.
/// </summary>
public interface IRememberMeStore
{
    /// <summary>
    /// Saves a remember-me token.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="expiresAt">The token expiry time.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveAsync(Guid userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a valid remembered user identifier.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The remembered user identifier when valid; otherwise, <see langword="null"/>.</returns>
    Task<Guid?> ReadUserIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the remember-me token.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
