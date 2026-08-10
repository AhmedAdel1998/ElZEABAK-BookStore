namespace BookStore.Domain.Tests;

public class ArchitectureTests
{
    [Fact]
    public void DomainProject_ContainsDomainAssembly()
    {
        Assert.Equal("BookStore.Domain", typeof(BookStore.Domain.Entities.Product).Assembly.GetName().Name);
    }
}
