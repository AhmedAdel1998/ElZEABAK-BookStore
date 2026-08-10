using BookStore.Application.Interfaces;

namespace BookStore.Infrastructure.Authentication;

/// <summary>
/// Hashes and verifies passwords with BCrypt.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    /// <inheritdoc />
    public bool NeedsRehash(string passwordHash)
    {
        return BCrypt.Net.BCrypt.PasswordNeedsRehash(passwordHash, WorkFactor);
    }
}
