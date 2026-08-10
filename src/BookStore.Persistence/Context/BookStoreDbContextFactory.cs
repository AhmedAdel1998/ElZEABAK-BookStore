using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookStore.Persistence.Context;

/// <summary>
/// Creates design-time database contexts for Entity Framework tooling.
/// </summary>
public class BookStoreDbContextFactory : IDesignTimeDbContextFactory<BookStoreDbContext>
{
    /// <inheritdoc />
    public BookStoreDbContext CreateDbContext(string[] args)
    {
        var databaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Database");
        Directory.CreateDirectory(databaseDirectory);
        var databasePath = Path.Combine(databaseDirectory, "bookstore.db");

        var optionsBuilder = new DbContextOptionsBuilder<BookStoreDbContext>();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");
        return new BookStoreDbContext(optionsBuilder.Options);
    }
}
