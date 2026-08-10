namespace BookStore.Application.Features.Authentication.Responses;

/// <summary>
/// Represents an authenticated user session.
/// </summary>
public class UserSessionSnapshot
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's role.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permissions assigned to the user.
    /// </summary>
    public IReadOnlyCollection<string> Permissions { get; set; } = [];

    /// <summary>
    /// Gets or sets the login time.
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }
}
