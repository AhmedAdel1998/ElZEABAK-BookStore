namespace BookStore.Application.Tests;

public class ArchitectureTests
{
    [Fact]
    public void ApplicationProject_ExposesDependencyInjectionRegistration()
    {
        Assert.NotNull(typeof(BookStore.Application.DependencyInjection));
    }
}
