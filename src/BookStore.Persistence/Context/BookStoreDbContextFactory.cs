using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.Sqlite;
using BookStore.Shared.Constants;

namespace BookStore.Persistence.Context;

/// <summary>
/// Creates design-time database contexts for Entity Framework tooling.
/// </summary>
public class BookStoreDbContextFactory : IDesignTimeDbContextFactory<BookStoreDbContext>
{
    /// <inheritdoc />
    public BookStoreDbContext CreateDbContext(string[] args)
    {
        var databaseDirectory = ApplicationPaths.ResolveDataPath("Database");
        Directory.CreateDirectory(databaseDirectory);
        var databasePath = Path.Combine(databaseDirectory, "bookstore.db");

        var optionsBuilder = new DbContextOptionsBuilder<BookStoreDbContext>();
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
            DefaultTimeout = 30
        }.ToString();

        optionsBuilder.UseSqlite(connectionString);
        return new BookStoreDbContext(optionsBuilder.Options);
    }
}
