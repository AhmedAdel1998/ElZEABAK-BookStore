using BookStore.Persistence.Context;
using BookStore.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookStore.Persistence.Seed;

/// <summary>
/// Applies migrations and seed data when the host starts.
/// </summary>
public class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseInitializer"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        Directory.CreateDirectory(ApplicationPaths.ResolveDataPath(FolderConstants.Database));
        await ConfigureSqliteRuntimeAsync(dbContext, cancellationToken);
        _logger.LogInformation("Applying database migrations");
        await dbContext.Database.MigrateAsync(cancellationToken);
        await seeder.SeedAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task ConfigureSqliteRuntimeAsync(BookStoreDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 30000;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
    }
}
