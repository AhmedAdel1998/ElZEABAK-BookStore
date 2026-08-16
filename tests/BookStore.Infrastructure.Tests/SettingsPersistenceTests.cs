using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Domain.Interfaces;
using BookStore.Persistence.Context;
using BookStore.Persistence.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Tests;

public class SettingsPersistenceTests
{
    [Fact]
    public async Task SettingsStore_PersistsAndUpdatesGroupRecords_InSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connection).Options;
        await using var context = new BookStoreDbContext(options);
        await context.Database.EnsureCreatedAsync();
        ISettingsStore store = new EfSettingsStore(context);

        await store.BeginTransactionAsync();
        await store.UpsertAsync(new PersistedSettingRecord("Settings.Tax", "{\"enabled\":true,\"defaultRate\":0.2,\"taxIncludedInPrice\":false,\"taxDisplayMode\":\"Separate\"}", SettingsCategory.Tax.ToString(), typeof(TaxSettingsDto).FullName!, "Tax", false, false, DateTimeOffset.UtcNow, "admin"));
        await store.CommitAsync();

        var saved = await store.GetByKeyAsync("Settings.Tax");
        Assert.NotNull(saved);
        Assert.Contains("0.2", saved!.Value, StringComparison.Ordinal);

        await store.BeginTransactionAsync();
        await store.UpsertAsync(saved with { Value = "{\"enabled\":true,\"defaultRate\":0.1,\"taxIncludedInPrice\":false,\"taxDisplayMode\":\"Separate\"}" });
        await store.CommitAsync();

        var updated = await store.GetByKeyAsync("Settings.Tax");
        Assert.Contains("0.1", updated!.Value, StringComparison.Ordinal);
        Assert.Single(await store.GetAllAsync());
    }
}
