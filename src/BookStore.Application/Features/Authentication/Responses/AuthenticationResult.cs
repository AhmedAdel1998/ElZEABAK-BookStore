using BookStore.Shared.Results;

namespace BookStore.Application.Features.Authentication.Responses;

/// <summary>
/// Represents an authentication operation result.
/// </summary>
public class AuthenticationResult : OperationResult
{
    /// <summary>
    /// Gets or sets the authenticated session when authentication succeeds.
    /// </summary>
    public UserSessionSnapshot? Session { get; set; }

    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    /// <param name="session">The authenticated session.</param>
    /// <returns>A successful authentication result.</returns>
    public static AuthenticationResult Authenticated(UserSessionSnapshot session)
    {
        return new AuthenticationResult { Succeeded = true, Session = session };
    }

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    /// <param name="message">The user-safe failure message.</param>
    /// <returns>A failed authentication result.</returns>
    public static AuthenticationResult Failed(string message)
    {
        return new AuthenticationResult
        {
            Succeeded = false,
            Errors = [new Error("Authentication.Failed", message)]
        };
    }
}
