using BookStore.Application.Features.Authentication.DTOs;
using BookStore.Application.Features.Authentication.Responses;
using BookStore.Shared.Results;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Provides authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with credentials.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The authentication result.</returns>
    Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to restore a remembered user session.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The authentication result.</returns>
    Task<AuthenticationResult> TryRestoreRememberedSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    /// <param name="request">The change password request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
