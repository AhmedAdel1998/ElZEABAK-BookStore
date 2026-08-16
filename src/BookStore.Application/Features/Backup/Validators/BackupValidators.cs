using BookStore.Application.Features.Backup.Commands;
using FluentValidation;

namespace BookStore.Application.Features.Backup.Validators;

public sealed class RestoreBackupCommandValidator : AbstractValidator<RestoreBackupCommand>
{
    public RestoreBackupCommandValidator()
    {
        RuleFor(command => command.BackupId).NotEmpty();
        RuleFor(command => command.ConfirmationText)
            .Equal("I understand that restoring will replace the current database.")
            .WithMessage("Restore confirmation text is required.");
    }
}

public sealed class DeleteBackupCommandValidator : AbstractValidator<DeleteBackupCommand>
{
    public DeleteBackupCommandValidator() => RuleFor(command => command.BackupId).NotEmpty();
}

public sealed class ValidateBackupCommandValidator : AbstractValidator<ValidateBackupCommand>
{
    public ValidateBackupCommandValidator() => RuleFor(command => command.BackupId).NotEmpty();
}
