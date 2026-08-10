using BookStore.Domain.Common;
using BookStore.Domain.Exceptions;
using BookStore.Domain.ValueObjects;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents an application user.
/// </summary>
public class User : BaseEntity, IAggregateRoot
{
    private User()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        FullName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="passwordHash">The BCrypt password hash.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="roleId">The role identifier.</param>
    public User(string username, string passwordHash, string fullName, Guid roleId)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ValidationException("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash) || !passwordHash.StartsWith("$2", StringComparison.Ordinal))
        {
            throw new ValidationException("A BCrypt password hash is required.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationException("User full name is required.");
        }

        Username = username.Trim();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        RoleId = roleId;
        FailedLoginCount = 0;
        IsActive = true;
    }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; private set; }

    /// <summary>
    /// Gets the password hash.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Gets the full name.
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public Email? Email { get; private set; }

    /// <summary>
    /// Gets the role identifier.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Gets the assigned role.
    /// </summary>
    public Role? Role { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets failed login count.
    /// </summary>
    public int FailedLoginCount { get; private set; }

    /// <summary>
    /// Gets the last failed login time.
    /// </summary>
    public DateTimeOffset? LastFailedLogin { get; private set; }

    /// <summary>
    /// Gets the account lockout end time.
    /// </summary>
    public DateTimeOffset? LockoutUntil { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the account is currently locked.
    /// </summary>
    public bool IsLockedOut => LockoutUntil.HasValue && LockoutUntil.Value > DateTimeOffset.UtcNow;

    /// <summary>
    /// Changes the password hash.
    /// </summary>
    /// <param name="passwordHash">The BCrypt password hash.</param>
    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || !passwordHash.StartsWith("$2", StringComparison.Ordinal))
        {
            throw new ValidationException("A BCrypt password hash is required.");
        }

        PasswordHash = passwordHash;
        MarkUpdated();
    }

    /// <summary>
    /// Updates the user's contact information.
    /// </summary>
    /// <param name="email">The email address.</param>
    public void UpdateContact(Email? email)
    {
        Email = email;
        MarkUpdated();
    }

    /// <summary>
    /// Registers a failed login attempt and locks the account when the limit is reached.
    /// </summary>
    /// <param name="maxAttempts">The maximum failed attempts before lockout.</param>
    /// <param name="lockoutDuration">The lockout duration.</param>
    public void RegisterFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginCount++;
        LastFailedLogin = DateTimeOffset.UtcNow;

        if (FailedLoginCount >= maxAttempts)
        {
            LockoutUntil = DateTimeOffset.UtcNow.Add(lockoutDuration);
        }

        MarkUpdated();
    }

    /// <summary>
    /// Resets failed login tracking after successful authentication.
    /// </summary>
    public void ResetFailedLogins()
    {
        FailedLoginCount = 0;
        LastFailedLogin = null;
        LockoutUntil = null;
        MarkUpdated();
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
