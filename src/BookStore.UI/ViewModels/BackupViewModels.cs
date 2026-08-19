using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using BookStore.Application.Features.Backup.Commands;
using BookStore.Application.Features.Backup.DTOs;
using BookStore.Application.Features.Backup.Handlers;
using BookStore.Application.Features.Backup.Queries;
using BookStore.Application.Interfaces;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

public partial class BackupListViewModel : BaseViewModel
{
    private readonly CreateBackupHandler _createBackupHandler;
    private readonly GetBackupsHandler _getBackupsHandler;
    private readonly ValidateBackupHandler _validateBackupHandler;
    private readonly RestoreBackupHandler _restoreBackupHandler;
    private readonly DeleteBackupHandler _deleteBackupHandler;
    private readonly CleanupBackupsHandler _cleanupBackupsHandler;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationDialogService _confirmationDialogService;

    [ObservableProperty] private BackupMetadataDto? selectedBackup;
    [ObservableProperty] private string restoreConfirmationText = string.Empty;

    public BackupListViewModel(
        CreateBackupHandler createBackupHandler,
        GetBackupsHandler getBackupsHandler,
        ValidateBackupHandler validateBackupHandler,
        RestoreBackupHandler restoreBackupHandler,
        DeleteBackupHandler deleteBackupHandler,
        CleanupBackupsHandler cleanupBackupsHandler,
        INotificationService notificationService,
        IConfirmationDialogService confirmationDialogService)
    {
        _createBackupHandler = createBackupHandler;
        _getBackupsHandler = getBackupsHandler;
        _validateBackupHandler = validateBackupHandler;
        _restoreBackupHandler = restoreBackupHandler;
        _deleteBackupHandler = deleteBackupHandler;
        _cleanupBackupsHandler = cleanupBackupsHandler;
        _notificationService = notificationService;
        _confirmationDialogService = confirmationDialogService;
        Title = "Backup";
        _ = LoadAsync();
    }

    /// <summary>The exact sentence a user must type before a restore is allowed.</summary>
    public const string RequiredRestoreConfirmation = "I understand that restoring will replace the current database.";

    /// <summary>
    /// Gets the phrase the user has to type. Surfaced so the screen can display it -- the backup list
    /// demanded an exact match while showing only an empty box, which made restore impossible there.
    /// </summary>
    public static string RequiredConfirmationText => RequiredRestoreConfirmation;

    public ObservableCollection<BackupMetadataDto> Backups { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        var result = await _getBackupsHandler.HandleAsync(new GetBackupsQuery());
        IsBusy = false;
        Backups.Clear();
        if (!result.IsSuccess || result.Value is null)
        {
            _notificationService.Show("Backup", result.Error ?? "Unable to load backups.", NotificationSeverity.Error);
            return;
        }

        foreach (var backup in result.Value)
        {
            Backups.Add(backup);
        }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (SelectedBackup is null || !await _confirmationDialogService.ConfirmAsync("Restore Backup", "Restoring will replace the current database. Continue?"))
        {
            return;
        }

        IsBusy = true;
        var result = await _restoreBackupHandler.HandleAsync(new RestoreBackupCommand(SelectedBackup.BackupId, RestoreConfirmationText));
        IsBusy = false;
        var operation = result.Value;
        _notificationService.Show("Restore", operation?.Message ?? result.Error ?? "Unable to restore backup.", operation?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        IsBusy = true;
        var result = await _createBackupHandler.HandleAsync(new CreateBackupCommand(BackupType.Manual));
        IsBusy = false;
        var operation = result.Value;
        _notificationService.Show("Backup", operation?.Message ?? result.Error ?? "Unable to create backup.", operation?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        if (SelectedBackup is null)
        {
            return;
        }

        IsBusy = true;
        var result = await _validateBackupHandler.HandleAsync(new ValidateBackupCommand(SelectedBackup.BackupId));
        IsBusy = false;
        var operation = result.Value;
        _notificationService.Show("Backup", operation?.Message ?? result.Error ?? "Unable to validate backup.", operation?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedBackup is null || !await _confirmationDialogService.ConfirmAsync("Delete Backup", "Delete the selected backup file and metadata?"))
        {
            return;
        }

        IsBusy = true;
        var result = await _deleteBackupHandler.HandleAsync(new DeleteBackupCommand(SelectedBackup.BackupId));
        IsBusy = false;
        var operation = result.Value;
        _notificationService.Show("Backup", operation?.Message ?? result.Error ?? "Unable to delete backup.", operation?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CleanupAsync()
    {
        IsBusy = true;
        var result = await _cleanupBackupsHandler.HandleAsync(new CleanupBackupsCommand());
        IsBusy = false;
        var operation = result.Value;
        _notificationService.Show("Backup", operation?.Message ?? result.Error ?? "Unable to clean up backups.", operation?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var folder = SelectedBackup is null ? null : Path.GetDirectoryName(SelectedBackup.FilePath);
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            _notificationService.Show("Backup", "Backup folder was not found.", NotificationSeverity.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }
}

public sealed class BackupViewModel : BackupListViewModel
{
    public BackupViewModel(
        CreateBackupHandler createBackupHandler,
        GetBackupsHandler getBackupsHandler,
        ValidateBackupHandler validateBackupHandler,
        RestoreBackupHandler restoreBackupHandler,
        DeleteBackupHandler deleteBackupHandler,
        CleanupBackupsHandler cleanupBackupsHandler,
        INotificationService notificationService,
        IConfirmationDialogService confirmationDialogService)
        : base(createBackupHandler, getBackupsHandler, validateBackupHandler, restoreBackupHandler, deleteBackupHandler, cleanupBackupsHandler, notificationService, confirmationDialogService)
    {
    }
}

public partial class BackupDetailsViewModel : BaseViewModel
{
    [ObservableProperty] private BackupMetadataDto? backup;

    public BackupDetailsViewModel()
    {
        Title = "Backup Details";
    }
}

public partial class BackupRestoreViewModel : BaseViewModel
{
    private readonly RestoreBackupHandler _restoreBackupHandler;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationDialogService _confirmationDialogService;

    [ObservableProperty] private BackupMetadataDto? selectedBackup;
    [ObservableProperty] private string confirmationText = string.Empty;

    public BackupRestoreViewModel(RestoreBackupHandler restoreBackupHandler, INotificationService notificationService, IConfirmationDialogService confirmationDialogService)
    {
        _restoreBackupHandler = restoreBackupHandler;
        _notificationService = notificationService;
        _confirmationDialogService = confirmationDialogService;
        Title = "Restore Backup";
    }

    /// <summary>Gets the phrase the user has to type before restore is permitted.</summary>
    public static string RequiredConfirmationText => BackupListViewModel.RequiredRestoreConfirmation;

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (SelectedBackup is null || !await _confirmationDialogService.ConfirmAsync("Restore Backup", "Restoring will replace the current database. Continue?"))
        {
            return;
        }

        IsBusy = true;
        var result = await _restoreBackupHandler.HandleAsync(new RestoreBackupCommand(SelectedBackup.BackupId, ConfirmationText));
        IsBusy = false;
        var operation = result.Value;
        _notificationService.Show("Restore", operation?.Message ?? result.Error ?? "Unable to restore backup.", operation?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
    }
}

public partial class DatabaseHealthViewModel : BaseViewModel
{
    private readonly GetDatabaseHealthHandler _healthHandler;
    private readonly RunIntegrityCheckHandler _integrityCheckHandler;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private DatabaseHealthResult? health;
    [ObservableProperty] private DatabaseIntegrityResult? integrity;

    public DatabaseHealthViewModel(GetDatabaseHealthHandler healthHandler, RunIntegrityCheckHandler integrityCheckHandler, INotificationService notificationService)
    {
        _healthHandler = healthHandler;
        _integrityCheckHandler = integrityCheckHandler;
        _notificationService = notificationService;
        Title = "Database Health";
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        var result = await _healthHandler.HandleAsync(new GetDatabaseHealthQuery());
        IsBusy = false;
        if (result.IsSuccess)
        {
            Health = result.Value;
        }
        else
        {
            _notificationService.Show("Database", result.Error ?? "Unable to load database health.", NotificationSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task RunIntegrityAsync()
    {
        IsBusy = true;
        var result = await _integrityCheckHandler.HandleAsync(new RunIntegrityCheckCommand());
        IsBusy = false;
        Integrity = result.Value;
        _notificationService.Show("Database", result.Value?.Message ?? result.Error ?? "Integrity check failed.", result.Value?.IsHealthy == true ? NotificationSeverity.Success : NotificationSeverity.Error);
    }
}
