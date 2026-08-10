namespace BookStore.Infrastructure.Tests;

public class ArchitectureTests
{
    [Fact]
    public void PasswordHasher_UsesBcryptHashFormat()
    {
        var hasher = new BookStore.Infrastructure.Authentication.PasswordHasher();

        var hash = hasher.HashPassword("password");

        Assert.StartsWith("$2", hash);
        Assert.True(hasher.VerifyPassword("password", hash));
    }
}
