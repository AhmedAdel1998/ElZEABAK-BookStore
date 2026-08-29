namespace BookStore.Infrastructure.Tests;

public class ArchitectureTests
{
    [Fact]
    public void PasswordHasher_UsesVersionedPreHashedBcryptFormat()
    {
        var hasher = new BookStore.Infrastructure.Authentication.PasswordHasher();

        var hash = hasher.HashPassword("password");

        Assert.StartsWith("BS2:$2", hash);
        Assert.True(hasher.VerifyPassword("password", hash));
    }
}
