using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using BookStore.Shared.Constants;

namespace BookStore.Infrastructure.Backup.Services;

public sealed class DatabasePathResolver
{
    private readonly IConfiguration _configuration;

    public DatabasePathResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetDatabasePath()
    {
        var connectionString = _configuration.GetConnectionString("BookStoreDb") ?? "Data Source=Database/bookstore.db";
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
        {
            throw new InvalidOperationException("A file-based SQLite database is required for backup operations.");
        }

        var expandedDataSource = Environment.ExpandEnvironmentVariables(builder.DataSource);
        return Path.GetFullPath(Path.IsPathRooted(expandedDataSource)
            ? expandedDataSource
            : ApplicationPaths.ResolveDataPath(expandedDataSource));
    }

    public static string BuildConnectionString(string databasePath)
    {
        return new SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 30 }.ToString();
    }
}
