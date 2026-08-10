namespace BookStore.Application.Features.Authentication.DTOs;

/// <summary>
/// Represents a login request.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the login should be remembered.
    /// </summary>
    public bool RememberMe { get; set; }
}
