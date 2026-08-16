using BookStore.Application.Features.Backup.Commands;
using BookStore.Application.Features.Backup.DTOs;
using BookStore.Application.Features.Backup.Handlers;
using BookStore.Application.Features.Backup.Services;
using BookStore.Application.Features.Backup.Validators;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Application.Tests;

public sealed class BackupModuleTests
{
    [Fact]
    public async Task Restore_RequiresBackupRestorePermission()
    {
        var handler = new RestoreBackupHandler(
            new FakeAuthorizationService([]),
            new FakeBackupService(),
            new RestoreBackupCommandValidator(),
            NullLogger<RestoreBackupHandler>.Instance);

        var result = await handler.HandleAsync(new RestoreBackupCommand(Guid.NewGuid(), "I understand that restoring will replace the current database."));

        Assert.False(result.IsSuccess);
        Assert.Equal("Current user cannot restore backups.", result.Error);
    }

    [Fact]
    public async Task CreateBackup_RequiresBackupCreatePermission()
    {
        var handler = new CreateBackupHandler(
            new FakeAuthorizationService([]),
            new FakeBackupService(),
            NullLogger<CreateBackupHandler>.Instance);

        var result = await handler.HandleAsync(new CreateBackupCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("Current user cannot create backups.", result.Error);
    }

    [Fact]
    public async Task Restore_RequiresExactConfirmationText()
    {
        var handler = new RestoreBackupHandler(
            new FakeAuthorizationService([PermissionConstants.BackupRestore]),
            new FakeBackupService(),
            new RestoreBackupCommandValidator(),
            NullLogger<RestoreBackupHandler>.Instance);

        var result = await handler.HandleAsync(new RestoreBackupCommand(Guid.NewGuid(), "restore"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Restore confirmation text is required.", result.Error);
    }

    private sealed class FakeBackupService : IBackupService
    {
        public Task<BackupOperationResult> CreateBackupAsync(BackupType backupType, CancellationToken cancellationToken = default) => Task.FromResult(BackupOperationResult.Success("Created."));
        public Task<IReadOnlyCollection<BackupMetadataDto>> ListBackupsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<BackupMetadataDto>>([]);
        public Task<BackupMetadataDto?> GetBackupAsync(Guid backupId, CancellationToken cancellationToken = default) => Task.FromResult<BackupMetadataDto?>(null);
        public Task<BackupOperationResult> ValidateBackupAsync(Guid backupId, CancellationToken cancellationToken = default) => Task.FromResult(BackupOperationResult.Success("Valid."));
        public Task<BackupOperationResult> RestoreBackupAsync(Guid backupId, string confirmationText, CancellationToken cancellationToken = default) => Task.FromResult(BackupOperationResult.Success("Restored.", restartRequired: true));
        public Task<BackupOperationResult> DeleteBackupAsync(Guid backupId, CancellationToken cancellationToken = default) => Task.FromResult(BackupOperationResult.Success("Deleted."));
        public Task<BackupOperationResult> CleanupBackupsAsync(CancellationToken cancellationToken = default) => Task.FromResult(BackupOperationResult.Success("Cleaned."));
    }

    private sealed class FakeAuthorizationService(IReadOnlyCollection<string> permissions) : IAuthorizationService
    {
        public bool HasPermission(string permission) => permissions.Contains(permission);
        public bool HasPermissions(params string[] permissionsToCheck) => permissionsToCheck.All(HasPermission);
        public bool HasRole(string role) => false;
        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
    }
}
