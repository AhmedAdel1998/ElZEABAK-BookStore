using System.Security.Cryptography;
using System.Text;
using BookStore.Application.Interfaces;

namespace BookStore.Infrastructure.Authentication;

/// <summary>
/// Hashes and verifies passwords with BCrypt.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;
    private const string PreHashedPrefix = "BS2:";

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return PreHashedPrefix + BCrypt.Net.BCrypt.HashPassword(PreHash(password), WorkFactor);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string passwordHash)
    {
        return passwordHash.StartsWith(PreHashedPrefix, StringComparison.Ordinal)
            ? BCrypt.Net.BCrypt.Verify(PreHash(password), passwordHash[PreHashedPrefix.Length..])
            : BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    /// <inheritdoc />
    public bool NeedsRehash(string passwordHash)
    {
        if (!passwordHash.StartsWith(PreHashedPrefix, StringComparison.Ordinal))
        {
            return true;
        }

        return BCrypt.Net.BCrypt.PasswordNeedsRehash(passwordHash[PreHashedPrefix.Length..], WorkFactor);
    }

    private static string PreHash(string password)
    {
        var digest = SHA384.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(digest);
    }
}
