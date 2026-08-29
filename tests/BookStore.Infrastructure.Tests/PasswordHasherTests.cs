using BookStore.Infrastructure.Authentication;

namespace BookStore.Infrastructure.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void VerifyPassword_DistinguishesPasswordsThatDifferAfterBcryptBoundary()
    {
        var hasher = new PasswordHasher();
        var sharedPrefix = "A1!" + new string('x', 69);
        var original = sharedPrefix + "original";
        var different = sharedPrefix + "different";

        var hash = hasher.HashPassword(original);

        Assert.True(hasher.VerifyPassword(original, hash));
        Assert.False(hasher.VerifyPassword(different, hash));
    }

    [Fact]
    public void VerifyPassword_AcceptsLegacyHashAndMarksItForUpgrade()
    {
        var hasher = new PasswordHasher();
        var legacyHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", 4);

        Assert.True(hasher.VerifyPassword("Admin123!", legacyHash));
        Assert.True(hasher.NeedsRehash(legacyHash));
    }

    [Fact]
    public void NeedsRehash_CurrentVersionAtRequiredCost_ReturnsFalse()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("Admin123!");

        Assert.StartsWith("BS2:$2", hash, StringComparison.Ordinal);
        Assert.False(hasher.NeedsRehash(hash));
    }
}
