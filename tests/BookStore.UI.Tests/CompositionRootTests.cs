using BookStore.Application;
using BookStore.Infrastructure;
using BookStore.Persistence;
using BookStore.Reporting;
using BookStore.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.UI.Tests;

public sealed class CompositionRootTests
{
    [Fact]
    public void ProductionServiceGraph_HasNoMissingOrAmbiguousDependencies()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BookStoreDb"] = "Data Source=:memory:"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.Configure<ApplicationSettings>(configuration.GetSection("Application"));
        services
            .AddApplication()
            .AddPersistence(configuration)
            .AddInfrastructure()
            .AddReporting()
            .AddPresentation();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.NotNull(provider);
    }
}
