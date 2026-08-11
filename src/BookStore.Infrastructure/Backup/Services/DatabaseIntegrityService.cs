using System.Diagnostics;
using BookStore.Application.Features.Backup.DTOs;
using BookStore.Application.Features.Backup.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Backup.Services;

public sealed class DatabaseIntegrityService : IDatabaseIntegrityService
{
    private static readonly string[] RequiredTables = ["Products", "Sales", "InventoryTransactions", "Users"];
    private readonly DatabasePathResolver _databasePathResolver;
    private readonly IDiskSpaceService _diskSpaceService;
    private readonly ILogger<DatabaseIntegrityService> _logger;

    public DatabaseIntegrityService(DatabasePathResolver databasePathResolver, IDiskSpaceService diskSpaceService, ILogger<DatabaseIntegrityService> logger)
    {
        _databasePathResolver = databasePathResolver;
        _diskSpaceService = diskSpaceService;
        _logger = logger;
    }

    public async Task<DatabaseIntegrityResult> CheckIntegrityAsync(string? databasePath = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = databasePath ?? _databasePathResolver.GetDatabasePath();
        try
        {
            var result = await RunIntegrityCheckAsync(path, cancellationToken);
            stopwatch.Stop();
            return new DatabaseIntegrityResult
            {
                IsHealthy = result.IsHealthy,
                Message = result.Message,
                DatabasePath = path,
                CheckedAt = DateTimeOffset.UtcNow,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database integrity check failed. DatabasePath={DatabasePath}", path);
            return new DatabaseIntegrityResult
            {
                IsHealthy = false,
                Message = "Database integrity check failed.",
                DatabasePath = path,
                CheckedAt = DateTimeOffset.UtcNow,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<DatabaseHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var path = _databasePathResolver.GetDatabasePath();
        var integrity = await CheckIntegrityAsync(path, cancellationToken);
        var schemaVersion = integrity.IsHealthy ? await GetUserVersionAsync(path, cancellationToken) : string.Empty;
        var folder = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory;

        return new DatabaseHealthResult
        {
            IsHealthy = integrity.IsHealthy && File.Exists(path),
            Message = integrity.Message,
            DatabasePath = path,
            SchemaVersion = schemaVersion,
            AvailableDiskSpaceBytes = _diskSpaceService.GetAvailableBytes(folder),
            CheckedAt = DateTimeOffset.UtcNow
        };
    }

    public static async Task<(bool IsHealthy, string Message)> RunIntegrityCheckAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(databasePath))
        {
            return (false, "Database file was not found.");
        }

        await using var connection = new SqliteConnection(DatabasePathResolver.BuildConnectionString(databasePath));
        await connection.OpenAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA integrity_check;";
            var value = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
            if (!string.Equals(value, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "SQLite integrity check failed.");
            }
        }

        foreach (var table in RequiredTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            command.Parameters.AddWithValue("$name", table);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (count == 0)
            {
                return (false, $"Required schema table is missing: {table}.");
            }
        }

        return (true, "Database integrity check passed.");
    }

    private static async Task<string> GetUserVersionAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(DatabasePathResolver.BuildConnectionString(databasePath));
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? "0";
    }
}
