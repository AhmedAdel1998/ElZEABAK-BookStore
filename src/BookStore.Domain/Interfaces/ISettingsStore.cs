namespace BookStore.Domain.Interfaces;

/// <summary>
/// Persists whitelisted settings groups.
/// </summary>
public interface ISettingsStore
{
    Task<IReadOnlyList<PersistedSettingRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PersistedSettingRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task UpsertAsync(PersistedSettingRecord setting, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a persisted settings group row.
/// </summary>
public sealed record PersistedSettingRecord(string Key, string Value, string Category, string DataType, string Description, bool IsEncrypted, bool IsSystemSetting, DateTimeOffset? UpdatedAt, string? UpdatedBy);
