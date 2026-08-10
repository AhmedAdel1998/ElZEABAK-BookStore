namespace BookStore.Shared.Models;

/// <summary>
/// Represents authentication and session settings.
/// </summary>
public class AuthenticationSettings
{
    /// <summary>
    /// Gets or sets the maximum failed login attempts before lockout.
    /// </summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the lockout duration in minutes.
    /// </summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the session timeout in minutes.
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the remember-me token lifetime in days.
    /// </summary>
    public int RememberMeDays { get; set; } = 30;
}
