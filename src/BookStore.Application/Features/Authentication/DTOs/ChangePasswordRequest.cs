namespace BookStore.Application.Features.Authentication.DTOs;

/// <summary>
/// Represents a change password request.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Gets or sets the current password.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirmation password.
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
