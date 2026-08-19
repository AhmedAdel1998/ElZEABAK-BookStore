using BookStore.Domain.Entities;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Tests;

/// <summary>
/// Pins the credential and lockout invariants login and account security depend on.
/// </summary>
public class UserTests
{
    // A syntactically plausible BCrypt hash; the entity only checks the "$2" prefix, it does not
    // verify a real hash.
    private const string ValidHash = "$2a$11$abcdefghijklmnopqrstuvABCDEFGHIJKLMNOPQRSTUV";

    private static User CreateUser() => new("cashier1", ValidHash, "Cashier One", Guid.NewGuid());

    [Fact]
    public void Constructor_RejectsBlankUsername()
    {
        Assert.Throws<ValidationException>(() => new User("   ", ValidHash, "Full Name", Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plaintext-password")]
    [InlineData("$1$notbcrypt")]
    public void Constructor_RejectsNonBCryptHash(string hash)
    {
        Assert.Throws<ValidationException>(() => new User("cashier1", hash, "Full Name", Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_RejectsBlankFullName()
    {
        Assert.Throws<ValidationException>(() => new User("cashier1", ValidHash, "   ", Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_TrimsUsernameAndFullName_StartsActiveWithNoFailures()
    {
        var user = new User("  cashier1  ", ValidHash, "  Cashier One  ", Guid.NewGuid());

        Assert.Equal("cashier1", user.Username);
        Assert.Equal("Cashier One", user.FullName);
        Assert.True(user.IsActive);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.False(user.IsLockedOut);
    }

    [Fact]
    public void ChangePasswordHash_RejectsNonBCryptHash()
    {
        var user = CreateUser();

        Assert.Throws<ValidationException>(() => user.ChangePasswordHash("not-bcrypt"));
    }

    [Fact]
    public void RegisterFailedLogin_BelowThreshold_DoesNotLockOut()
    {
        var user = CreateUser();

        user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        Assert.Equal(1, user.FailedLoginCount);
        Assert.False(user.IsLockedOut);
        Assert.Null(user.LockoutUntil);
    }

    [Fact]
    public void RegisterFailedLogin_ReachingThreshold_LocksAccount()
    {
        var user = CreateUser();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        }

        Assert.Equal(5, user.FailedLoginCount);
        Assert.True(user.IsLockedOut);
        Assert.NotNull(user.LockoutUntil);
    }

    [Fact]
    public void ResetFailedLogins_ClearsCountAndLockout()
    {
        var user = CreateUser();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        }

        user.ResetFailedLogins();

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LastFailedLogin);
        Assert.Null(user.LockoutUntil);
        Assert.False(user.IsLockedOut);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = CreateUser();

        user.Deactivate();

        Assert.False(user.IsActive);
    }
}
