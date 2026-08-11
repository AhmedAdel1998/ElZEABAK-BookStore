using BookStore.Application.Features.Backup.Commands;
using BookStore.Application.Features.Backup.DTOs;
using BookStore.Application.Features.Backup.Queries;
using BookStore.Application.Features.Backup.Services;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Backup.Handlers;

public sealed class CreateBackupHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IBackupService _backupService;
    private readonly ILogger<CreateBackupHandler> _logger;

    public CreateBackupHandler(IAuthorizationService authorizationService, IBackupService backupService, ILogger<CreateBackupHandler> logger)
    {
        _authorizationService = authorizationService;
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<Result<BackupOperationResult>> HandleAsync(CreateBackupCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.BackupCreate))
        {
            _logger.LogWarning("Unauthorized backup create request.");
            return Result<BackupOperationResult>.Failure("Current user cannot create backups.");
        }

        return Result<BackupOperationResult>.Success(await _backupService.CreateBackupAsync(command.BackupType, cancellationToken));
    }
}

public sealed class RestoreBackupHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IBackupService _backupService;
    private readonly IValidator<RestoreBackupCommand> _validator;
    private readonly ILogger<RestoreBackupHandler> _logger;

    public RestoreBackupHandler(IAuthorizationService authorizationService, IBackupService backupService, IValidator<RestoreBackupCommand> validator, ILogger<RestoreBackupHandler> logger)
    {
        _authorizationService = authorizationService;
        _backupService = backupService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<BackupOperationResult>> HandleAsync(RestoreBackupCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.BackupRestore))
        {
            _logger.LogWarning("Unauthorized backup restore request. BackupId={BackupId}", command.BackupId);
            return Result<BackupOperationResult>.Failure("Current user cannot restore backups.");
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<BackupOperationResult>.Failure(validation.Errors[0].ErrorMessage);
        }

        return Result<BackupOperationResult>.Success(await _backupService.RestoreBackupAsync(command.BackupId, command.ConfirmationText, cancellationToken));
    }
}

public sealed class DeleteBackupHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IBackupService _backupService;
    private readonly IValidator<DeleteBackupCommand> _validator;

    public DeleteBackupHandler(IAuthorizationService authorizationService, IBackupService backupService, IValidator<DeleteBackupCommand> validator)
    {
        _authorizationService = authorizationService;
        _backupService = backupService;
        _validator = validator;
    }

    public async Task<Result<BackupOperationResult>> HandleAsync(DeleteBackupCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.BackupDelete))
        {
            return Result<BackupOperationResult>.Failure("Current user cannot delete backups.");
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        return validation.IsValid
            ? Result<BackupOperationResult>.Success(await _backupService.DeleteBackupAsync(command.BackupId, cancellationToken))
            : Result<BackupOperationResult>.Failure(validation.Errors[0].ErrorMessage);
    }
}

public sealed class CleanupBackupsHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IBackupService _backupService;

    public CleanupBackupsHandler(IAuthorizationService authorizationService, IBackupService backupService)
    {
        _authorizationService = authorizationService;
        _backupService = backupService;
    }

    public async Task<Result<BackupOperationResult>> HandleAsync(CleanupBackupsCommand command, CancellationToken cancellationToken = default)
    {
        return _authorizationService.HasPermission(PermissionConstants.BackupDelete)
            ? Result<BackupOperationResult>.Success(await _backupService.CleanupBackupsAsync(cancellationToken))
            : Result<BackupOperationResult>.Failure("Current user cannot clean up backups.");
    }
}

public sealed class ValidateBackupHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IBackupService _backupService;
    private readonly IValidator<ValidateBackupCommand> _validator;

    public ValidateBackupHandler(IAuthorizationService authorizationService, IBackupService backupService, IValidator<ValidateBackupCommand> validator)
    {
        _authorizationService = authorizationService;
        _backupService = backupService;
        _validator = validator;
    }

    public async Task<Result<BackupOperationResult>> HandleAsync(ValidateBackupCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.BackupValidate))
        {
            return Result<BackupOperationResult>.Failure("Current user cannot validate backups.");
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        return validation.IsValid
            ? Result<BackupOperationResult>.Success(await _backupService.ValidateBackupAsync(command.BackupId, cancellationToken))
            : Result<BackupOperationResult>.Failure(validation.Errors[0].ErrorMessage);
    }
}

public sealed class GetBackupsHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IBackupService _backupService;

    public GetBackupsHandler(IAuthorizationService authorizationService, IBackupService backupService)
    {
        _authorizationService = authorizationService;
        _backupService = backupService;
    }

    public async Task<Result<IReadOnlyCollection<BackupMetadataDto>>> HandleAsync(GetBackupsQuery query, CancellationToken cancellationToken = default)
    {
        return _authorizationService.HasPermission(PermissionConstants.BackupView)
            ? Result<IReadOnlyCollection<BackupMetadataDto>>.Success(await _backupService.ListBackupsAsync(cancellationToken))
            : Result<IReadOnlyCollection<BackupMetadataDto>>.Failure("Current user cannot view backups.");
    }
}

public sealed class GetBackupDetailsHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IBackupService _backupService;

    public GetBackupDetailsHandler(IAuthorizationService authorizationService, IBackupService backupService)
    {
        _authorizationService = authorizationService;
        _backupService = backupService;
    }

    public async Task<Result<BackupMetadataDto>> HandleAsync(GetBackupDetailsQuery query, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.BackupView))
        {
            return Result<BackupMetadataDto>.Failure("Current user cannot view backup details.");
        }

        var backup = await _backupService.GetBackupAsync(query.BackupId, cancellationToken);
        return backup is null
            ? Result<BackupMetadataDto>.Failure("Backup was not found.")
            : Result<BackupMetadataDto>.Success(backup);
    }
}

public sealed class RunIntegrityCheckHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IDatabaseIntegrityService _integrityService;

    public RunIntegrityCheckHandler(IAuthorizationService authorizationService, IDatabaseIntegrityService integrityService)
    {
        _authorizationService = authorizationService;
        _integrityService = integrityService;
    }

    public async Task<Result<DatabaseIntegrityResult>> HandleAsync(RunIntegrityCheckCommand command, CancellationToken cancellationToken = default)
    {
        return _authorizationService.HasPermission(PermissionConstants.BackupValidate)
            ? Result<DatabaseIntegrityResult>.Success(await _integrityService.CheckIntegrityAsync(cancellationToken: cancellationToken))
            : Result<DatabaseIntegrityResult>.Failure("Current user cannot run database integrity checks.");
    }
}

public sealed class GetDatabaseHealthHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IDatabaseIntegrityService _integrityService;

    public GetDatabaseHealthHandler(IAuthorizationService authorizationService, IDatabaseIntegrityService integrityService)
    {
        _authorizationService = authorizationService;
        _integrityService = integrityService;
    }

    public async Task<Result<DatabaseHealthResult>> HandleAsync(GetDatabaseHealthQuery query, CancellationToken cancellationToken = default)
    {
        return _authorizationService.HasPermission(PermissionConstants.BackupView)
            ? Result<DatabaseHealthResult>.Success(await _integrityService.CheckHealthAsync(cancellationToken))
            : Result<DatabaseHealthResult>.Failure("Current user cannot view database health.");
    }
}

public sealed class RunAutomaticBackupHandler
{
    private readonly IAutomaticBackupService _automaticBackupService;

    public RunAutomaticBackupHandler(IAutomaticBackupService automaticBackupService)
    {
        _automaticBackupService = automaticBackupService;
    }

    public async Task<Result<BackupOperationResult>> HandleAsync(RunAutomaticBackupCommand command, CancellationToken cancellationToken = default)
    {
        return Result<BackupOperationResult>.Success(await _automaticBackupService.RunIfDueAsync(cancellationToken));
    }
}
