using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookStore.Persistence.Settings;

/// <summary>
/// Stores persistent settings overrides in the bookstore database.
/// </summary>
public sealed class EfSettingsStore : ISettingsStore
{
    private readonly BookStoreDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public EfSettingsStore(BookStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PersistedSettingRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ApplicationSettings
            .AsNoTracking()
            .OrderBy(setting => setting.Category)
            .ThenBy(setting => setting.Key)
            .Select(setting => new PersistedSettingRecord(setting.Key, setting.Value, setting.Category, setting.DataType, setting.Description, setting.IsEncrypted, setting.IsSystemSetting, setting.UpdatedAt, setting.UpdatedBy))
            .ToListAsync(cancellationToken);
    }

    public async Task<PersistedSettingRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ApplicationSettings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => new PersistedSettingRecord(setting.Key, setting.Value, setting.Category, setting.DataType, setting.Description, setting.IsEncrypted, setting.IsSystemSetting, setting.UpdatedAt, setting.UpdatedBy))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(PersistedSettingRecord setting, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ApplicationSettings.SingleOrDefaultAsync(item => item.Key == setting.Key, cancellationToken);
        if (entity is null)
        {
            await _dbContext.ApplicationSettings.AddAsync(new ApplicationSetting(setting.Key, setting.Value, setting.Category, setting.DataType, setting.Description, setting.IsEncrypted, setting.IsSystemSetting, setting.UpdatedBy), cancellationToken);
            return;
        }

        entity.Update(setting.Value, setting.DataType, setting.Description, setting.IsEncrypted, setting.IsSystemSetting, setting.UpdatedBy);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ApplicationSettings.SingleOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (entity is not null)
        {
            _dbContext.ApplicationSettings.Remove(entity);
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction ??= await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
