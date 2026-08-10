using BookStore.Application.Features.Authentication.Responses;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Stores the current authenticated user session.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets a value indicating whether a user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user identifier.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current username.
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Gets the current user's full name.
    /// </summary>
    string? FullName { get; }

    /// <summary>
    /// Gets the current user's role.
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Gets current user permissions.
    /// </summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    Guid? SessionId { get; }

    /// <summary>
    /// Gets the login time.
    /// </summary>
    DateTimeOffset? LoginTime { get; }

    /// <summary>
    /// Starts a new user session.
    /// </summary>
    /// <param name="session">The session to store.</param>
    void SignIn(UserSessionSnapshot session);

    /// <summary>
    /// Clears the current session.
    /// </summary>
    void SignOut();
}
