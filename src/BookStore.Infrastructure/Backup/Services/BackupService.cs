using System.Diagnostics;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Features.Settings.DTOs;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using BookStore.Application.Features.Backup.DTOs;
using BookStore.Application.Features.Backup.Services;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Backup.Services;

public sealed class BackupService : IBackupService
{
    private const string ConfirmationText = "I understand that restoring will replace the current database.";
    private static readonly SemaphoreSlim OperationLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly DatabasePathResolver _databasePathResolver;
    private readonly IDiskSpaceService _diskSpaceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly BackupSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<BackupService> _logger;

    // The administrator-configured settings, refreshed at the start of each operation. appsettings
    // remains the fallback for a first run or an unreadable settings table.
    private BackupSettingsDto? _stored;

    public BackupService(
        DatabasePathResolver databasePathResolver,
        IDiskSpaceService diskSpaceService,
        ICurrentUserService currentUserService,
        IOptions<ApplicationSettings> settings,
        ISettingsService settingsService,
        ILogger<BackupService> logger)
    {
        _databasePathResolver = databasePathResolver;
        _diskSpaceService = diskSpaceService;
        _currentUserService = currentUserService;
        _settings = settings.Value.Backup;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<BackupOperationResult> CreateBackupAsync(BackupType backupType, CancellationToken cancellationToken = default)
    {
        await RefreshStoredSettingsAsync(cancellationToken);
        await OperationLock.WaitAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var databasePath = _databasePathResolver.GetDatabasePath();
            if (!File.Exists(databasePath))
            {
                return BackupOperationResult.Failure("Database file was not found.");
            }

            var backupFolder = GetBackupFolder();
            Directory.CreateDirectory(backupFolder);
            var databaseSize = new FileInfo(databasePath).Length;
            var requiredSpace = Math.Max(databaseSize * 2, 10 * 1024 * 1024);
            if (!_diskSpaceService.HasEnoughSpace(backupFolder, requiredSpace))
            {
                return BackupOperationResult.Failure("Insufficient disk space to create a database backup.");
            }

            var metadata = new BackupMetadataDto
            {
                BackupId = Guid.NewGuid(),
                FileName = BuildFileName(backupType),
                CreatedAt = DateTimeOffset.UtcNow,
                BackupType = backupType,
                ApplicationVersion = GetApplicationVersion(),
                DatabaseVersion = await GetDatabaseVersionAsync(databasePath, cancellationToken),
                CreatedBy = _currentUserService.Username ?? "System",
                MachineName = Environment.MachineName
            };
            metadata.FilePath = Path.Combine(backupFolder, metadata.FileName);

            _logger.LogInformation("Backup started. BackupId={BackupId} Type={BackupType} User={User} Machine={Machine}", metadata.BackupId, backupType, metadata.CreatedBy, metadata.MachineName);
            await CreateSqliteBackupAsync(databasePath, metadata.FilePath, cancellationToken);
            metadata.FileSizeBytes = new FileInfo(metadata.FilePath).Length;
            metadata.ChecksumSha256 = await CalculateChecksumAsync(metadata.FilePath, cancellationToken);

            var validation = await ValidateFileAsync(metadata, requireChecksumMatch: true, cancellationToken);
            metadata.ValidationStatus = validation.Succeeded ? BackupValidationStatus.Valid : BackupValidationStatus.Invalid;
            metadata.ValidationMessage = validation.Message;
            await SaveMetadataAsync(metadata, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Backup completed. BackupId={BackupId} Result={Result} Duration={Duration}", metadata.BackupId, metadata.ValidationStatus, stopwatch.Elapsed);
            return validation.Succeeded
                ? BackupOperationResult.Success("Backup created and validated successfully.", metadata)
                : BackupOperationResult.Failure("Backup was created but validation failed.", metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed. User={User} Machine={Machine}", _currentUserService.Username, Environment.MachineName);
            return BackupOperationResult.Failure("Unable to create database backup.");
        }
        finally
        {
            OperationLock.Release();
        }
    }

    public async Task<IReadOnlyCollection<BackupMetadataDto>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        await RefreshStoredSettingsAsync(cancellationToken);
        var folder = GetBackupFolder();
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var backups = new List<BackupMetadataDto>();
        foreach (var metadataPath in Directory.EnumerateFiles(folder, "*.meta.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metadata = await ReadMetadataAsync(metadataPath, cancellationToken);
            if (metadata is not null)
            {
                backups.Add(metadata);
            }
        }

        return backups.OrderByDescending(backup => backup.CreatedAt).ToArray();
    }

    public async Task<BackupMetadataDto?> GetBackupAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        return (await ListBackupsAsync(cancellationToken)).FirstOrDefault(backup => backup.BackupId == backupId);
    }

    public async Task<BackupOperationResult> ValidateBackupAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        await RefreshStoredSettingsAsync(cancellationToken);
        var metadata = await GetBackupAsync(backupId, cancellationToken);
        if (metadata is null)
        {
            return BackupOperationResult.Failure("Backup was not found.");
        }

        var result = await ValidateFileAsync(metadata, requireChecksumMatch: true, cancellationToken);
        metadata.ValidationStatus = result.Succeeded ? BackupValidationStatus.Valid : BackupValidationStatus.Invalid;
        metadata.ValidationMessage = result.Message;
        metadata.FileSizeBytes = File.Exists(metadata.FilePath) ? new FileInfo(metadata.FilePath).Length : 0;
        if (result.Succeeded)
        {
            metadata.ChecksumSha256 = await CalculateChecksumAsync(metadata.FilePath, cancellationToken);
        }

        await SaveMetadataAsync(metadata, cancellationToken);
        _logger.LogInformation("Backup validation completed. BackupId={BackupId} Result={Result}", metadata.BackupId, metadata.ValidationStatus);
        return result.Succeeded
            ? BackupOperationResult.Success("Backup validation passed.", metadata)
            : BackupOperationResult.Failure("Backup validation failed.", metadata);
    }

    public async Task<BackupOperationResult> RestoreBackupAsync(Guid backupId, string confirmationText, CancellationToken cancellationToken = default)
    {
        await RefreshStoredSettingsAsync(cancellationToken);
        if (!string.Equals(confirmationText, ConfirmationText, StringComparison.Ordinal))
        {
            return BackupOperationResult.Failure("Restore confirmation text is required.");
        }

        await OperationLock.WaitAsync(cancellationToken);
        try
        {
            var selected = await GetBackupAsync(backupId, cancellationToken);
            if (selected is null)
            {
                return BackupOperationResult.Failure("Backup was not found.");
            }

            var selectedValidation = await ValidateFileAsync(selected, requireChecksumMatch: true, cancellationToken);
            if (!selectedValidation.Succeeded)
            {
                return BackupOperationResult.Failure("Selected backup is invalid and cannot be restored.", selected);
            }

            var databasePath = _databasePathResolver.GetDatabasePath();
            var databaseFolder = Path.GetDirectoryName(databasePath) ?? AppContext.BaseDirectory;
            var currentSize = File.Exists(databasePath) ? new FileInfo(databasePath).Length : 0;
            var selectedSize = new FileInfo(selected.FilePath).Length;
            var tempFolder = GetTempFolder();
            Directory.CreateDirectory(tempFolder);
            if (!_diskSpaceService.HasEnoughSpace(tempFolder, Math.Max((currentSize + selectedSize) * 2, 20 * 1024 * 1024)))
            {
                return BackupOperationResult.Failure("Insufficient disk space to restore the selected backup.", selected);
            }

            var preRestore = CreatePreRestoreBackup
                ? await CreateBackupInternalWithoutLockAsync(BackupType.PreRestore, cancellationToken)
                : null;
            if (CreatePreRestoreBackup && preRestore?.ValidationStatus != BackupValidationStatus.Valid)
            {
                return BackupOperationResult.Failure("Pre-restore safety backup could not be created.");
            }

            var tempRestore = Path.Combine(tempFolder, $"bookstore-restore-{Guid.NewGuid():N}.dbtmp");
            File.Copy(selected.FilePath, tempRestore, overwrite: false);
            var tempValidation = await DatabaseIntegrityService.RunIntegrityCheckAsync(tempRestore, cancellationToken);
            if (!tempValidation.IsHealthy)
            {
                SafeDelete(tempRestore);
                return BackupOperationResult.Failure("Temporary restore validation failed.", selected);
            }

            _logger.LogWarning("Restore started. BackupId={BackupId} User={User} Machine={Machine}", selected.BackupId, _currentUserService.Username, Environment.MachineName);
            Directory.CreateDirectory(databaseFolder);
            SqliteConnection.ClearAllPools();
            var rollbackCopy = Path.Combine(tempFolder, $"bookstore-rollback-{Guid.NewGuid():N}.dbtmp");

            try
            {
                if (File.Exists(databasePath))
                {
                    File.Replace(tempRestore, databasePath, rollbackCopy, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(tempRestore, databasePath);
                }

                var restoredValidation = await DatabaseIntegrityService.RunIntegrityCheckAsync(databasePath, cancellationToken);
                if (!restoredValidation.IsHealthy)
                {
                    throw new InvalidOperationException(restoredValidation.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Restore replacement failed. BackupId={BackupId}", selected.BackupId);
                if (File.Exists(rollbackCopy))
                {
                    File.Copy(rollbackCopy, databasePath, overwrite: true);
                }
                else if (preRestore is not null && File.Exists(preRestore.FilePath))
                {
                    File.Copy(preRestore.FilePath, databasePath, overwrite: true);
                }

                return BackupOperationResult.Failure("Restore failed. The previous database was preserved or recovered.", selected);
            }
            finally
            {
                SafeDelete(tempRestore);
                SafeDelete(rollbackCopy);
            }

            _logger.LogWarning("Restore completed. BackupId={BackupId} User={User}", selected.BackupId, _currentUserService.Username);
            return BackupOperationResult.Success("Database restored successfully. Restart the application before continuing work.", selected, restartRequired: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed. BackupId={BackupId}", backupId);
            return BackupOperationResult.Failure("Unable to restore the selected backup.");
        }
        finally
        {
            OperationLock.Release();
        }
    }

    public async Task<BackupOperationResult> DeleteBackupAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        await RefreshStoredSettingsAsync(cancellationToken);
        var backups = await ListBackupsAsync(cancellationToken);
        var backup = backups.FirstOrDefault(item => item.BackupId == backupId);
        if (backup is null)
        {
            return BackupOperationResult.Failure("Backup was not found.");
        }

        var validBackups = backups.Where(item => item.ValidationStatus == BackupValidationStatus.Valid && item.BackupId != backupId).ToArray();
        if (backup.ValidationStatus == BackupValidationStatus.Valid && validBackups.Length == 0)
        {
            return BackupOperationResult.Failure("Cannot delete the only valid backup.");
        }

        SafeDelete(backup.FilePath);
        SafeDelete(GetMetadataPath(backup.FilePath));
        _logger.LogInformation("Backup deleted. BackupId={BackupId} User={User}", backup.BackupId, _currentUserService.Username);
        return BackupOperationResult.Success("Backup deleted.", backup);
    }

    public async Task<BackupOperationResult> CleanupBackupsAsync(CancellationToken cancellationToken = default)
    {
        await RefreshStoredSettingsAsync(cancellationToken);
        var retention = Math.Max(1, RetentionCount);
        var backups = (await ListBackupsAsync(cancellationToken))
            .Where(backup => backup.BackupType != BackupType.PreRestore)
            .OrderByDescending(backup => backup.CreatedAt)
            .ToArray();
        var newestValid = backups.FirstOrDefault(backup => backup.ValidationStatus == BackupValidationStatus.Valid);
        var deleteCandidates = backups.Skip(retention).Where(backup => backup.BackupId != newestValid?.BackupId).ToArray();
        var deleted = 0;
        foreach (var backup in deleteCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafeDelete(backup.FilePath);
            SafeDelete(GetMetadataPath(backup.FilePath));
            deleted++;
        }

        return BackupOperationResult.Success($"Backup cleanup completed. Deleted {deleted} backup(s).");
    }

    private async Task<BackupMetadataDto> CreateBackupInternalWithoutLockAsync(BackupType backupType, CancellationToken cancellationToken)
    {
        var databasePath = _databasePathResolver.GetDatabasePath();
        var metadata = new BackupMetadataDto
        {
            BackupId = Guid.NewGuid(),
            FileName = BuildFileName(backupType),
            CreatedAt = DateTimeOffset.UtcNow,
            BackupType = backupType,
            ApplicationVersion = GetApplicationVersion(),
            DatabaseVersion = await GetDatabaseVersionAsync(databasePath, cancellationToken),
            CreatedBy = _currentUserService.Username ?? "System",
            MachineName = Environment.MachineName
        };
        metadata.FilePath = Path.Combine(GetBackupFolder(), metadata.FileName);

        Directory.CreateDirectory(GetBackupFolder());
        await CreateSqliteBackupAsync(databasePath, metadata.FilePath, cancellationToken);
        metadata.FileSizeBytes = new FileInfo(metadata.FilePath).Length;
        metadata.ChecksumSha256 = await CalculateChecksumAsync(metadata.FilePath, cancellationToken);
        var validation = await ValidateFileAsync(metadata, requireChecksumMatch: true, cancellationToken);
        metadata.ValidationStatus = validation.Succeeded ? BackupValidationStatus.Valid : BackupValidationStatus.Invalid;
        metadata.ValidationMessage = validation.Message;
        await SaveMetadataAsync(metadata, cancellationToken);
        _logger.LogInformation("PreRestore backup created. BackupId={BackupId} Result={Result}", metadata.BackupId, metadata.ValidationStatus);
        return metadata;
    }

    private static async Task CreateSqliteBackupAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        await using var source = new SqliteConnection(DatabasePathResolver.BuildConnectionString(sourcePath));
        await using var target = new SqliteConnection(DatabasePathResolver.BuildConnectionString(targetPath));
        await source.OpenAsync(cancellationToken);
        await target.OpenAsync(cancellationToken);
        source.BackupDatabase(target);
        SqliteConnection.ClearAllPools();
    }

    private async Task<BackupOperationResult> ValidateFileAsync(BackupMetadataDto metadata, bool requireChecksumMatch, CancellationToken cancellationToken)
    {
        if (!File.Exists(metadata.FilePath))
        {
            return BackupOperationResult.Failure("Backup file was not found.", metadata);
        }

        try
        {
            await using (File.Open(metadata.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
            }

            var checksum = await CalculateChecksumAsync(metadata.FilePath, cancellationToken);
            if (requireChecksumMatch && !string.IsNullOrWhiteSpace(metadata.ChecksumSha256) && !string.Equals(checksum, metadata.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
            {
                return BackupOperationResult.Failure("Backup checksum does not match metadata.", metadata);
            }

            var integrity = await DatabaseIntegrityService.RunIntegrityCheckAsync(metadata.FilePath, cancellationToken);
            if (!integrity.IsHealthy)
            {
                return BackupOperationResult.Failure(integrity.Message, metadata);
            }

            return BackupOperationResult.Success("Backup validation passed.", metadata);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Backup validation failed. BackupId={BackupId}", metadata.BackupId);
            return BackupOperationResult.Failure("Backup validation failed.", metadata);
        }
    }

    /// <summary>Gets the configured pre-restore backup behaviour.</summary>
    private bool CreatePreRestoreBackup => _stored?.CreatePreRestoreBackup ?? _settings.CreatePreRestoreBackup;

    /// <summary>Gets the configured number of backups to retain.</summary>
    private int RetentionCount => _stored is { RetentionCount: > 0 } stored ? stored.RetentionCount : _settings.RetentionCount;

    /// <summary>
    /// Reloads the administrator-configured backup settings, keeping the appsettings values if they
    /// cannot be read.
    /// </summary>
    private async Task RefreshStoredSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _stored = await _settingsService.GetAsync<BackupSettingsDto>(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Stored backup settings could not be read; using configured defaults.");
        }
    }

    private string GetBackupFolder() => ExpandConfiguredPath(
        string.IsNullOrWhiteSpace(_stored?.BackupLocation) ? _settings.Folder : _stored!.BackupLocation,
        "Backups");

    private string GetTempFolder() => ExpandConfiguredPath(_settings.TempFolder, "Temp");

    private static string ExpandConfiguredPath(string? configuredPath, string fallbackFolder)
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(programData, "BookStore", fallbackFolder)
            : configuredPath.Replace("%ProgramData%", programData, StringComparison.OrdinalIgnoreCase);
        // A relative configured path used to land in the read-only install directory. Rooted paths --
        // which every default and documented setting uses -- are unaffected.
        return ApplicationPaths.ResolveDataPath(path);
    }

    private static string BuildFileName(BackupType backupType)
    {
        var prefix = backupType == BackupType.PreRestore ? "BookStore_PreRestore" : "BookStore";
        return $"{prefix}_{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}_{Guid.NewGuid():N}.db";
    }

    private static string GetMetadataPath(string backupFilePath) => backupFilePath + ".meta.json";

    private static async Task SaveMetadataAsync(BackupMetadataDto metadata, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(GetMetadataPath(metadata.FilePath), JsonSerializer.Serialize(metadata, JsonOptions), cancellationToken);
    }

    private static async Task<BackupMetadataDto?> ReadMetadataAsync(string metadataPath, CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            return JsonSerializer.Deserialize<BackupMetadataDto>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string> CalculateChecksumAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<string> GetDatabaseVersionAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(DatabasePathResolver.BuildConnectionString(databasePath));
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? "0";
    }

    private static string GetApplicationVersion() => Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "1.0.0";

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
