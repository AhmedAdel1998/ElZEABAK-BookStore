namespace BookStore.Application.Interfaces;

/// <summary>
/// Hashes and verifies passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The password hash.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plain text password against a hash.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="passwordHash">The password hash.</param>
    /// <returns><see langword="true"/> when the password is valid; otherwise, <see langword="false"/>.</returns>
    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Determines whether the hash should be upgraded.
    /// </summary>
    /// <param name="passwordHash">The password hash.</param>
    /// <returns><see langword="true"/> when the hash should be rehashed; otherwise, <see langword="false"/>.</returns>
    bool NeedsRehash(string passwordHash);
}
