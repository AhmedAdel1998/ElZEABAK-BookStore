using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

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

        return Path.GetFullPath(Path.IsPathRooted(builder.DataSource)
            ? builder.DataSource
            : Path.Combine(AppContext.BaseDirectory, builder.DataSource));
    }

    public static string BuildConnectionString(string databasePath)
    {
        return new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
    }
}
