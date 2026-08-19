using BookStore.Application.Features.Authentication.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Features.Audit.DTOs;
using BookStore.Application.Features.Audit.Services;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Models;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Authentication;

/// <summary>
/// Provides secure authentication operations.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRememberMeStore _rememberMeStore;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly AuthenticationSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IAuditTrailService? _auditTrailService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
    /// </summary>
    public AuthenticationService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        IRememberMeStore rememberMeStore,
        IValidator<LoginRequest> loginValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IOptions<ApplicationSettings> options,
        ISettingsService settingsService,
        ILogger<AuthenticationService> logger,
        IAuditTrailService? auditTrailService = null)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _rememberMeStore = rememberMeStore;
        _loginValidator = loginValidator;
        _changePasswordValidator = changePasswordValidator;
        _settings = options.Value.Authentication;
        _settingsService = settingsService;
        _logger = logger;
        _auditTrailService = auditTrailService;
    }

    /// <summary>
    /// Reads the lockout policy an administrator configured in Settings, falling back to the
    /// appsettings values if the stored settings cannot be read.
    /// </summary>
    private async Task<(int MaxAttempts, int LockoutMinutes)> ReadSecuritySettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var security = await _settingsService.GetAsync<SecuritySettingsDto>(cancellationToken);
            return (Math.Max(security.MaxLoginAttempts, 1), Math.Max(security.LockoutDuration, 1));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Stored security settings could not be read; using configured defaults for lockout.");
            return (Math.Max(_settings.MaxFailedLoginAttempts, 1), Math.Max(_settings.LockoutMinutes, 1));
        }
    }

    /// <inheritdoc />
    public async Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return AuthenticationResult.Failed(string.Join(Environment.NewLine, validation.Errors.Select(error => error.ErrorMessage)));
        }

        var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Failed login for unknown user.");
            await RecordAuditAsync("Authentication", "Login failed", "Failed", null, request.Username, "Unknown username", cancellationToken);
            return AuthenticationResult.Failed(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Failed login for inactive user. UserId={UserId}", user.Id);
            await RecordAuditAsync("Authentication", "Login failed", "Failed", user.Id, user.Username, "Inactive account", cancellationToken);
            return AuthenticationResult.Failed("This account is inactive.");
        }

        if (user.IsLockedOut)
        {
            _logger.LogWarning("Locked account login attempt. UserId={UserId}", user.Id);
            await RecordAuditAsync("Authentication", "Login failed", "Failed", user.Id, user.Username, "Locked account", cancellationToken);
            return AuthenticationResult.Failed($"This account is locked until {user.LockoutUntil:yyyy-MM-dd HH:mm}.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            var security = await ReadSecuritySettingsAsync(cancellationToken);
            user.RegisterFailedLogin(security.MaxAttempts, TimeSpan.FromMinutes(security.LockoutMinutes));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (user.IsLockedOut)
            {
                _logger.LogWarning("Account locked. UserId={UserId}", user.Id);
                await RecordAuditAsync("Authentication", "Account locked", "Failed", user.Id, user.Username, "Too many failed login attempts", cancellationToken);
                return AuthenticationResult.Failed("Too many failed login attempts. The account has been temporarily locked.");
            }

            _logger.LogWarning("Failed login for user. UserId={UserId}", user.Id);
            await RecordAuditAsync("Authentication", "Login failed", "Failed", user.Id, user.Username, "Invalid password", cancellationToken);
            return AuthenticationResult.Failed(InvalidCredentialsMessage);
        }

        user.ResetFailedLogins();
        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.ChangePasswordHash(_passwordHasher.HashPassword(request.Password));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var session = CreateSession(user);
        _currentUserService.SignIn(session);

        if (request.RememberMe)
        {
            await _rememberMeStore.SaveAsync(user.Id, DateTimeOffset.UtcNow.AddDays(_settings.RememberMeDays), cancellationToken);
        }
        else
        {
            await _rememberMeStore.ClearAsync(cancellationToken);
        }

        _logger.LogInformation("Successful login. UserId={UserId}", user.Id);
        await RecordAuditAsync("Authentication", "Login", "Succeeded", user.Id, user.Username, request.RememberMe ? "Remember me enabled" : null, cancellationToken);
        return AuthenticationResult.Authenticated(session);
    }

    /// <inheritdoc />
    public async Task<AuthenticationResult> TryRestoreRememberedSessionAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _rememberMeStore.ReadUserIdAsync(cancellationToken);
        if (!userId.HasValue)
        {
            return AuthenticationResult.Failed("No remembered session was found.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || !user.IsActive || user.IsLockedOut)
        {
            await _rememberMeStore.ClearAsync(cancellationToken);
            return AuthenticationResult.Failed("Remembered session is no longer valid.");
        }

        var session = CreateSession(user);
        _currentUserService.SignIn(session);
        _logger.LogInformation("Remembered session restored. UserId={UserId}", user.Id);
        await RecordAuditAsync("Authentication", "Remembered session restored", "Succeeded", user.Id, user.Username, null, cancellationToken);
        return AuthenticationResult.Authenticated(session);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        var username = _currentUserService.Username;
        _currentUserService.SignOut();
        await _rememberMeStore.ClearAsync(cancellationToken);
        _logger.LogInformation("Logout. UserId={UserId}", userId);
        await RecordAuditAsync("Authentication", "Logout", "Succeeded", userId, username, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return OperationResult.Failure(new Error("Authentication.NotAuthenticated", "You must be logged in to change your password."));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(_currentUserService.UserId.Value, cancellationToken);
        if (user is null)
        {
            return OperationResult.Failure(new Error("Authentication.UserMissing", "The current user could not be loaded."));
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password change failed because current password was invalid. UserId={UserId}", user.Id);
            return OperationResult.Failure(new Error("Authentication.InvalidPassword", "Current password is invalid."));
        }

        user.ChangePasswordHash(_passwordHasher.HashPassword(request.NewPassword));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Password changed. UserId={UserId}", user.Id);
        await RecordAuditAsync("Authentication", "Password changed", "Succeeded", user.Id, user.Username, null, cancellationToken);
        return OperationResult.Success();
    }

    private async Task RecordAuditAsync(string area, string action, string outcome, Guid? userId, string? username, string? detail, CancellationToken cancellationToken)
    {
        if (_auditTrailService is null)
        {
            return;
        }

        try
        {
            await _auditTrailService.RecordAsync(new AuditLogRequest(area, action, outcome, userId, username, "User", userId, detail), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit trail write failed for {Area} {Action}", area, action);
        }
    }

    private static UserSessionSnapshot CreateSession(User user)
    {
        return new UserSessionSnapshot
        {
            SessionId = Guid.NewGuid(),
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role?.Name ?? string.Empty,
            Permissions = user.Role?.Permissions.Select(permission => permission.Name).ToArray() ?? [],
            LoginTime = DateTimeOffset.UtcNow
        };
    }
}
