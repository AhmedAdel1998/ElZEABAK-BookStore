using BookStore.Application.Features.Backup.DTOs;
using BookStore.Application.Features.Backup.Services;
using BookStore.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Backup.Services;

public sealed class AutomaticBackupService : IAutomaticBackupService
{
    private readonly IBackupService _backupService;
    private readonly BackupSettings _settings;
    private readonly ILogger<AutomaticBackupService> _logger;

    public AutomaticBackupService(IBackupService backupService, IOptions<ApplicationSettings> settings, ILogger<AutomaticBackupService> logger)
    {
        _backupService = backupService;
        _settings = settings.Value.Backup;
        _logger = logger;
    }

    public async Task<BackupOperationResult> RunIfDueAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || string.Equals(_settings.Frequency, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return BackupOperationResult.Success("Automatic backup is disabled.");
        }

        var backups = await _backupService.ListBackupsAsync(cancellationToken);
        var latestAutomatic = backups
            .Where(backup => backup.BackupType == BackupType.Automatic && backup.ValidationStatus == BackupValidationStatus.Valid)
            .OrderByDescending(backup => backup.CreatedAt)
            .FirstOrDefault();
        var dueAfter = string.Equals(_settings.Frequency, "Weekly", StringComparison.OrdinalIgnoreCase) ? TimeSpan.FromDays(7) : TimeSpan.FromDays(1);
        if (latestAutomatic is not null && DateTimeOffset.UtcNow - latestAutomatic.CreatedAt < dueAfter)
        {
            return BackupOperationResult.Success("Automatic backup is not due yet.", latestAutomatic);
        }

        _logger.LogInformation("Automatic backup started.");
        var result = await _backupService.CreateBackupAsync(BackupType.Automatic, cancellationToken);
        await _backupService.CleanupBackupsAsync(cancellationToken);
        return result;
    }
}
