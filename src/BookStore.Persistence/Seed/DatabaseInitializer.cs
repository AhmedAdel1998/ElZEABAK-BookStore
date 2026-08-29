using BookStore.Persistence.Context;
using BookStore.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Data;
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
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pendingMigrations.Length > 0)
        {
            var migrationBackup = await CreatePreMigrationBackupAsync(dbContext, cancellationToken);
            _logger.LogInformation("Applying {MigrationCount} database migrations. PreMigrationBackup={PreMigrationBackup}", pendingMigrations.Length, migrationBackup ?? "Not required for a new or in-memory database");
            await dbContext.Database.MigrateAsync(cancellationToken);
            await VerifyDatabaseAfterMigrationAsync(dbContext, cancellationToken);
        }
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

    private static async Task<string?> CreatePreMigrationBackupAsync(BookStoreDbContext dbContext, CancellationToken cancellationToken)
    {
        var source = (SqliteConnection)dbContext.Database.GetDbConnection();
        if (string.IsNullOrWhiteSpace(source.DataSource) || source.DataSource == ":memory:" || !File.Exists(source.DataSource) || new FileInfo(source.DataSource).Length == 0)
        {
            return null;
        }

        var backupDirectory = Path.Combine(Path.GetDirectoryName(source.DataSource)!, "MigrationBackups");
        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(backupDirectory, $"bookstore-pre-migration-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.db");
        var openedHere = source.State != ConnectionState.Open;
        if (openedHere) await source.OpenAsync(cancellationToken);
        try
        {
            await using var destination = new SqliteConnection($"Data Source={backupPath};Mode=ReadWriteCreate");
            await destination.OpenAsync(cancellationToken);
            source.BackupDatabase(destination);
        }
        finally
        {
            if (openedHere) await source.CloseAsync();
        }
        return backupPath;
    }

    private static async Task VerifyDatabaseAfterMigrationAsync(BookStoreDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var integrityCommand = connection.CreateCommand();
            integrityCommand.CommandText = "PRAGMA quick_check(1);";
            var result = Convert.ToString(await integrityCommand.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"Database integrity check failed after migration: {result}");

            await using var foreignKeyCommand = connection.CreateCommand();
            foreignKeyCommand.CommandText = "PRAGMA foreign_key_check;";
            await using var violations = await foreignKeyCommand.ExecuteReaderAsync(cancellationToken);
            if (await violations.ReadAsync(cancellationToken)) throw new InvalidDataException("Foreign-key validation failed after database migration.");
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }
}
