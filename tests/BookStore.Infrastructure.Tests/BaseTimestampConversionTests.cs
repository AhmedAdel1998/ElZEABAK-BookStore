using BookStore.Domain.Entities;
using BookStore.Persistence.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Infrastructure.Tests;

/// <summary>
/// Verifies the fix for the dormant trap shared by every entity's <c>CreatedAt</c>/
/// <c>UpdatedAt</c> columns (and the two <c>User</c> lockout timestamps): before the
/// <c>ConvertBaseTimestampsToTicks</c> migration, these were persisted as raw
/// <see cref="DateTimeOffset"/> text, which SQLite cannot compare or sort - the exact failure mode
/// that broke the audit trail (see <see cref="QueryTranslationSweepTests"/>), just not yet hit by
/// any caller. These tests exercise the query shapes that would have thrown, and confirm the
/// migration does not corrupt a timestamp already stored under the old TEXT representation.
/// </summary>
public class BaseTimestampConversionTests
{
    [Fact]
    public async Task CreatedAt_SupportsRangeFilteringAndOrdering()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();

        // CreatedAt is stamped by BaseEntity's constructor from DateTimeOffset.UtcNow with no
        // settable override, and Windows' default timer resolution (~15ms) can give two
        // back-to-back entities the identical tick value. A real gap between them is required for
        // the ordering assertion below to mean anything rather than depend on tie-break luck.
        fixture.Context.Categories.Add(new Category("Fiction"));
        await fixture.Context.SaveChangesAsync();
        await Task.Delay(20);
        fixture.Context.Categories.Add(new Category("Non-Fiction"));
        await fixture.Context.SaveChangesAsync();

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-5);

        // This is precisely the query shape ("filter on a base timestamp") that had no caller yet
        // but would have thrown "could not be translated" the moment one was written, exactly as
        // happened with AuditLogEntry.OccurredAt.
        var recent = await fixture.Context.Categories.AsNoTracking()
            .Where(category => category.CreatedAt >= cutoff)
            .CountAsync();
        Assert.Equal(2, recent);

        var ordered = await fixture.Context.Categories.AsNoTracking()
            .OrderByDescending(category => category.CreatedAt)
            .Select(category => category.Name)
            .ToListAsync();
        Assert.Equal(["Non-Fiction", "Fiction"], ordered);
    }

    [Fact]
    public async Task UpdatedAt_RoundTripsThroughSaveAndReload()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var category = new Category("Fiction");
        fixture.Context.Categories.Add(category);
        await fixture.Context.SaveChangesAsync();

        category.Rename("Fiction & Fantasy");
        await fixture.Context.SaveChangesAsync();
        var expectedUpdatedAt = category.UpdatedAt;
        fixture.Context.ChangeTracker.Clear();

        var reloaded = await fixture.Context.Categories.AsNoTracking().SingleAsync(c => c.Id == category.Id);

        Assert.NotNull(expectedUpdatedAt);
        Assert.NotNull(reloaded.UpdatedAt);
        Assert.Equal(expectedUpdatedAt.Value.UtcTicks, reloaded.UpdatedAt.Value.UtcTicks);
    }

    [Fact]
    public async Task Migration_PreservesTimestampsAlreadyStoredAsText()
    {
        // Simulates upgrading an existing installation: writes a Categories row with CreatedAt
        // stored the OLD way (raw ISO-8601 TEXT, as every pre-migration database has it), then
        // applies the migration and confirms the value survives to the millisecond - the same
        // guarantee already proven for AuditLogEntry.OccurredAt in production.
        var path = Path.Combine(Path.GetTempPath(), $"basetimestamp-upgrade-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={path}";

        try
        {
            var original = new DateTimeOffset(2026, 3, 14, 9, 26, 53, 987, TimeSpan.Zero);

            await using (var preMigration = new BookStoreDbContext(
                new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connectionString).Options))
            {
                // Stops one migration short of the one under test, so the table still has the
                // original TEXT-column schema this migration is responsible for converting.
                await preMigration.GetInfrastructure().GetRequiredService<IMigrator>()
                    .MigrateAsync("AddReportAnalyticsIndexes");

                await using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                await using var insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO Categories (Id, Name, IsActive, CreatedAt, IsDeleted) " +
                                      "VALUES ($id, 'Legacy Category', 1, $createdAt, 0)";
                insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                insert.Parameters.AddWithValue("$createdAt", original.ToString("O"));
                await insert.ExecuteNonQueryAsync();
            }

            await using (var afterMigration = new BookStoreDbContext(
                new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connectionString).Options))
            {
                await afterMigration.Database.MigrateAsync();

                var migrated = await afterMigration.Categories.AsNoTracking().SingleAsync(c => c.Name == "Legacy Category");
                Assert.Equal(original.UtcTicks, migrated.CreatedAt.UtcTicks);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var candidate in new[] { path, path + "-wal", path + "-shm" })
            {
                if (File.Exists(candidate))
                {
                    File.Delete(candidate);
                }
            }
        }
    }

    private sealed class PersistenceFixture : IAsyncDisposable
    {
        private PersistenceFixture(SqliteConnection connection, BookStoreDbContext context)
        {
            Connection = connection;
            Context = context;
        }

        private SqliteConnection Connection { get; }

        public BookStoreDbContext Context { get; }

        public static async Task<PersistenceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connection).Options;
            var context = new BookStoreDbContext(options);
            await context.Database.MigrateAsync();
            return new PersistenceFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
