using BookStore.Application.Features.Backup.DTOs;

namespace BookStore.Application.Features.Backup.Services;

public interface IBackupService
{
    Task<BackupOperationResult> CreateBackupAsync(BackupType backupType, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BackupMetadataDto>> ListBackupsAsync(CancellationToken cancellationToken = default);
    Task<BackupMetadataDto?> GetBackupAsync(Guid backupId, CancellationToken cancellationToken = default);
    Task<BackupOperationResult> ValidateBackupAsync(Guid backupId, CancellationToken cancellationToken = default);
    Task<BackupOperationResult> RestoreBackupAsync(Guid backupId, string confirmationText, CancellationToken cancellationToken = default);
    Task<BackupOperationResult> DeleteBackupAsync(Guid backupId, CancellationToken cancellationToken = default);
    Task<BackupOperationResult> CleanupBackupsAsync(CancellationToken cancellationToken = default);
}

public interface IDatabaseIntegrityService
{
    Task<DatabaseIntegrityResult> CheckIntegrityAsync(string? databasePath = null, CancellationToken cancellationToken = default);
    Task<DatabaseHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public interface IDiskSpaceService
{
    bool HasEnoughSpace(string folderPath, long requiredBytes);
    long GetAvailableBytes(string folderPath);
}

public interface IAutomaticBackupService
{
    Task<BackupOperationResult> RunIfDueAsync(CancellationToken cancellationToken = default);
}
