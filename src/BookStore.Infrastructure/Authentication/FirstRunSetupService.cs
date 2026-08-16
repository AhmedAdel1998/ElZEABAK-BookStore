using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Results;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Authentication;

/// <summary>
/// Handles secure first-run administrator creation.
/// </summary>
public sealed class FirstRunSetupService : IFirstRunSetupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<FirstRunSetupService> _logger;

    public FirstRunSetupService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ILogger<FirstRunSetupService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.ListAsync(null, cancellationToken);
        return users.Count == 0;
    }

    public async Task<OperationResult> CreateAdministratorAsync(string username, string fullName, string email, string password, string confirmPassword, CancellationToken cancellationToken = default)
    {
        if (!await IsSetupRequiredAsync(cancellationToken))
        {
            return OperationResult.Failure(new Error("FirstRun.AlreadyConfigured", "Initial setup has already been completed."));
        }

        var errors = Validate(username, fullName, email, password, confirmPassword);
        if (errors.Count > 0)
        {
            return OperationResult.Invalid(errors.ToArray());
        }

        var roles = await _unitOfWork.Roles.ListAsync(null, cancellationToken);
        var administratorRole = roles.FirstOrDefault(role => string.Equals(role.Name, "Administrator", StringComparison.OrdinalIgnoreCase));
        if (administratorRole is null)
        {
            return OperationResult.Failure(new Error("FirstRun.AdministratorRoleMissing", "Administrator role was not found."));
        }

        var user = new User(username.Trim(), _passwordHasher.HashPassword(password), fullName.Trim(), administratorRole.Id);
        user.UpdateContact(new Email(email.Trim()));
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initial administrator account created. Username={Username}", user.Username);
        return OperationResult.Success();
    }

    private static List<ValidationError> Validate(string username, string fullName, string email, string password, string confirmPassword)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
        {
            errors.Add(new ValidationError(nameof(username), "Username is required and must be 50 characters or fewer."));
        }

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
        {
            errors.Add(new ValidationError(nameof(fullName), "Full name is required and must be 150 characters or fewer."));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal) || email.Length > 254)
        {
            errors.Add(new ValidationError(nameof(email), "A valid email address is required."));
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit))
        {
            errors.Add(new ValidationError(nameof(password), "Password must be at least 8 characters and include uppercase, lowercase, number, and special character."));
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            errors.Add(new ValidationError(nameof(confirmPassword), "Passwords must match."));
        }

        return errors;
    }
}
