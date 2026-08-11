using BookStore.Application.Features.Backup.DTOs;

namespace BookStore.Application.Features.Backup.Commands;

public sealed record CreateBackupCommand(BackupType BackupType = BackupType.Manual);

public sealed record RestoreBackupCommand(Guid BackupId, string ConfirmationText);

public sealed record DeleteBackupCommand(Guid BackupId);

public sealed record CleanupBackupsCommand;

public sealed record ValidateBackupCommand(Guid BackupId);

public sealed record RunIntegrityCheckCommand;

public sealed record RunAutomaticBackupCommand;
