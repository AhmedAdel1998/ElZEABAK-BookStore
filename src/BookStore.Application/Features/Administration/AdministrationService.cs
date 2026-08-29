using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Exceptions;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Administration;

/// <summary>Represents a user row in administration screens.</summary>
public sealed record UserAdministrationDto(Guid Id, string Username, string FullName, string? Email, Guid RoleId, string RoleName, bool IsActive, bool IsLockedOut);

/// <summary>Represents a role and its assigned permissions.</summary>
public sealed record RoleAdministrationDto(Guid Id, string Name, string? Description, IReadOnlyCollection<string> Permissions);

/// <summary>Represents an assignable permission.</summary>
public sealed record PermissionAdministrationDto(Guid Id, string Name, string? Description);

/// <summary>Creates a user account.</summary>
public sealed record CreateUserAdministrationRequest(string Username, string FullName, string? Email, Guid RoleId, string Password);

/// <summary>Updates a user account.</summary>
public sealed record UpdateUserAdministrationRequest(Guid Id, string Username, string FullName, string? Email, Guid RoleId);

/// <summary>Creates or updates a role.</summary>
public sealed record SaveRoleAdministrationRequest(Guid? Id, string Name, string? Description, IReadOnlyCollection<Guid> PermissionIds);

/// <summary>Provides protected user and role administration operations.</summary>
public sealed class AdministrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdministrationService> _logger;

    /// <summary>Initializes the administration service.</summary>
    public AdministrationService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IAuthorizationService authorizationService, ICurrentUserService currentUserService, ILogger<AdministrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>Gets all users.</summary>
    public async Task<Result<IReadOnlyCollection<UserAdministrationDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        if (!Can(PermissionConstants.UsersManage)) return Result<IReadOnlyCollection<UserAdministrationDto>>.Failure("You do not have permission to manage users.");
        var users = await _unitOfWork.Users.ListAsync(cancellationToken: cancellationToken);
        return Result<IReadOnlyCollection<UserAdministrationDto>>.Success(users.OrderBy(user => user.Username).Select(MapUser).ToArray());
    }

    /// <summary>Gets all roles.</summary>
    public async Task<Result<IReadOnlyCollection<RoleAdministrationDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        if (!Can(PermissionConstants.UsersManage) && !Can(PermissionConstants.RolesManage)) return Result<IReadOnlyCollection<RoleAdministrationDto>>.Failure("You do not have permission to view roles.");
        var roles = await _unitOfWork.Roles.ListAsync(cancellationToken: cancellationToken);
        return Result<IReadOnlyCollection<RoleAdministrationDto>>.Success(roles.OrderBy(role => role.Name).Select(MapRole).ToArray());
    }

    /// <summary>Gets all permissions.</summary>
    public async Task<Result<IReadOnlyCollection<PermissionAdministrationDto>>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (!Can(PermissionConstants.RolesManage)) return Result<IReadOnlyCollection<PermissionAdministrationDto>>.Failure("You do not have permission to manage roles.");
        var permissions = await _unitOfWork.Roles.ListPermissionsAsync(cancellationToken);
        return Result<IReadOnlyCollection<PermissionAdministrationDto>>.Success(permissions.Select(permission => new PermissionAdministrationDto(permission.Id, permission.Name, permission.Description)).ToArray());
    }

    /// <summary>Creates a user.</summary>
    public async Task<Result> CreateUserAsync(CreateUserAdministrationRequest request, CancellationToken cancellationToken = default)
    {
        if (!Can(PermissionConstants.UsersManage)) return Result.Failure("You do not have permission to manage users.");
        var validation = ValidateUser(request.Username, request.FullName, request.Email, request.RoleId, request.Password);
        if (validation is not null) return Result.Failure(validation);
        if (await _unitOfWork.Users.ExistsByUsernameAsync(request.Username, cancellationToken: cancellationToken)) return Result.Failure("Username is already in use.");
        if (await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken) is null) return Result.Failure("The selected role does not exist.");
        try
        {
            var user = new User(request.Username, _passwordHasher.HashPassword(request.Password), request.FullName, request.RoleId);
            user.UpdateContact(CreateEmail(request.Email));
            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User created. UserId={UserId} Username={Username} Actor={Actor}", user.Id, user.Username, _currentUserService.Username);
            return Result.Success();
        }
        catch (ValidationException ex) { return Result.Failure(ex.Message); }
    }

    /// <summary>Updates a user.</summary>
    public async Task<Result> UpdateUserAsync(UpdateUserAdministrationRequest request, CancellationToken cancellationToken = default)
    {
        if (!Can(PermissionConstants.UsersManage)) return Result.Failure("You do not have permission to manage users.");
        var validation = ValidateUser(request.Username, request.FullName, request.Email, request.RoleId, password: null);
        if (validation is not null) return Result.Failure(validation);
        var user = await _unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);
        if (user is null) return Result.Failure("User was not found.");
        if (await _unitOfWork.Users.ExistsByUsernameAsync(request.Username, request.Id, cancellationToken)) return Result.Failure("Username is already in use.");
        if (await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken) is null) return Result.Failure("The selected role does not exist.");
        try
        {
            user.UpdateProfile(request.Username, request.FullName, request.RoleId, CreateEmail(request.Email));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User updated. UserId={UserId} Actor={Actor}", user.Id, _currentUserService.Username);
            return Result.Success();
        }
        catch (ValidationException ex) { return Result.Failure(ex.Message); }
    }

    /// <summary>Activates or deactivates a user.</summary>
    public async Task<Result> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!Can(PermissionConstants.UsersManage)) return Result.Failure("You do not have permission to manage users.");
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null) return Result.Failure("User was not found.");
        if (!isActive && user.Id == _currentUserService.UserId) return Result.Failure("You cannot deactivate your own active session.");
        if (!isActive && user.Role?.Permissions.Any(permission => permission.Name == PermissionConstants.UsersManage) == true)
        {
            var users = await _unitOfWork.Users.ListAsync(cancellationToken: cancellationToken);
            if (users.Count(candidate => candidate.IsActive && candidate.Role?.Permissions.Any(permission => permission.Name == PermissionConstants.UsersManage) == true) <= 1)
            {
                return Result.Failure("The last active user manager cannot be deactivated.");
            }
        }
        if (isActive) user.Activate(); else user.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User status changed. UserId={UserId} Active={Active} Actor={Actor}", user.Id, isActive, _currentUserService.Username);
        return Result.Success();
    }

    /// <summary>Resets a user's password and unlocks the account.</summary>
    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        if (!Can(PermissionConstants.UsersManage)) return Result.Failure("You do not have permission to manage users.");
        if (!IsValidPassword(newPassword)) return Result.Failure("Password must be 8-256 characters and include upper-case, lower-case, and numeric characters.");
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null) return Result.Failure("User was not found.");
        user.ChangePasswordHash(_passwordHasher.HashPassword(newPassword));
        user.ResetFailedLogins();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User password reset. UserId={UserId} Actor={Actor}", user.Id, _currentUserService.Username);
        return Result.Success();
    }

    /// <summary>Creates or updates a role and its complete permission set.</summary>
    public async Task<Result> SaveRoleAsync(SaveRoleAdministrationRequest request, CancellationToken cancellationToken = default)
    {
        if (!Can(PermissionConstants.RolesManage)) return Result.Failure("You do not have permission to manage roles.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100) return Result.Failure("Role name is required and cannot exceed 100 characters.");
        if (request.Description?.Trim().Length > 500) return Result.Failure("Role description cannot exceed 500 characters.");
        if (await _unitOfWork.Roles.ExistsByNameAsync(request.Name, request.Id, cancellationToken)) return Result.Failure("Role name is already in use.");
        var permissions = await _unitOfWork.Roles.ListPermissionsAsync(cancellationToken);
        var selected = permissions.Where(permission => request.PermissionIds.Contains(permission.Id)).ToArray();
        if (selected.Length != request.PermissionIds.Distinct().Count()) return Result.Failure("One or more selected permissions do not exist.");
        Role role;
        if (request.Id.HasValue)
        {
            var existingRole = await _unitOfWork.Roles.GetByIdAsync(request.Id.Value, cancellationToken);
            if (existingRole is null) return Result.Failure("Role was not found.");
            role = existingRole;
            if (await WouldRemoveLastManagerAsync(role, selected, cancellationToken)) return Result.Failure("This change would remove required management access from the last active administrator.");
            role.Update(request.Name, request.Description);
        }
        else
        {
            role = new Role(request.Name, request.Description);
            await _unitOfWork.Roles.AddAsync(role, cancellationToken);
        }
        role.SetPermissions(selected);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Role saved. RoleId={RoleId} Actor={Actor}", role.Id, _currentUserService.Username);
        return Result.Success();
    }

    private async Task<bool> WouldRemoveLastManagerAsync(Role role, IReadOnlyCollection<Permission> selected, CancellationToken cancellationToken)
    {
        var removesUsers = role.Permissions.Any(permission => permission.Name == PermissionConstants.UsersManage) && selected.All(permission => permission.Name != PermissionConstants.UsersManage);
        var removesRoles = role.Permissions.Any(permission => permission.Name == PermissionConstants.RolesManage) && selected.All(permission => permission.Name != PermissionConstants.RolesManage);
        if (!removesUsers && !removesRoles) return false;
        var users = await _unitOfWork.Users.ListAsync(cancellationToken: cancellationToken);
        bool HasOther(string permission) => users.Any(user => user.IsActive && user.RoleId != role.Id && user.Role?.Permissions.Any(item => item.Name == permission) == true);
        return (removesUsers && !HasOther(PermissionConstants.UsersManage)) || (removesRoles && !HasOther(PermissionConstants.RolesManage));
    }

    private bool Can(string permission) => _authorizationService.HasPermission(permission);
    private static UserAdministrationDto MapUser(User user) => new(user.Id, user.Username, user.FullName, user.Email?.Value, user.RoleId, user.Role?.Name ?? "Unknown", user.IsActive, user.IsLockedOut);
    private static RoleAdministrationDto MapRole(Role role) => new(role.Id, role.Name, role.Description, role.Permissions.Select(permission => permission.Name).OrderBy(name => name).ToArray());
    private static Email? CreateEmail(string? value) => string.IsNullOrWhiteSpace(value) ? null : new Email(value);
    private static string? ValidateUser(string username, string fullName, string? email, Guid roleId, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Trim().Length > 100) return "Username is required and cannot exceed 100 characters.";
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 200) return "Full name is required and cannot exceed 200 characters.";
        if (roleId == Guid.Empty) return "A role is required.";
        try { _ = CreateEmail(email); } catch (ValidationException ex) { return ex.Message; }
        return password is not null && !IsValidPassword(password) ? "Password must be 8-256 characters and include upper-case, lower-case, and numeric characters." : null;
    }
    private static bool IsValidPassword(string value) => value.Length is >= 8 and <= 256 && value.Any(char.IsUpper) && value.Any(char.IsLower) && value.Any(char.IsDigit);
}
