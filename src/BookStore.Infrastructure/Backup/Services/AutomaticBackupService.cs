using BookStore.Application.Features.Backup.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Backup.Services;
using BookStore.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Backup.Services;

public sealed class AutomaticBackupService : IAutomaticBackupService
{
    private readonly IBackupService _backupService;
    private readonly BackupSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AutomaticBackupService> _logger;

    public AutomaticBackupService(IBackupService backupService, IOptions<ApplicationSettings> settings, ISettingsService settingsService, ILogger<AutomaticBackupService> logger)
    {
        _backupService = backupService;
        _settings = settings.Value.Backup;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<BackupOperationResult> RunIfDueAsync(CancellationToken cancellationToken = default)
    {
        // The schedule an administrator chose in Settings, not the one baked into appsettings.
        var enabled = _settings.Enabled;
        var frequency = _settings.Frequency;
        try
        {
            var stored = await _settingsService.GetAsync<BackupSettingsDto>(cancellationToken);
            enabled = stored.Enabled;
            frequency = string.IsNullOrWhiteSpace(stored.Frequency) ? frequency : stored.Frequency;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Stored backup settings could not be read; using configured schedule.");
        }

        if (!enabled || string.Equals(frequency, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return BackupOperationResult.Success("Automatic backup is disabled.");
        }

        var backups = await _backupService.ListBackupsAsync(cancellationToken);
        var latestAutomatic = backups
            .Where(backup => backup.BackupType == BackupType.Automatic && backup.ValidationStatus == BackupValidationStatus.Valid)
            .OrderByDescending(backup => backup.CreatedAt)
            .FirstOrDefault();
        var dueAfter = string.Equals(frequency, "Weekly", StringComparison.OrdinalIgnoreCase) ? TimeSpan.FromDays(7) : TimeSpan.FromDays(1);
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
