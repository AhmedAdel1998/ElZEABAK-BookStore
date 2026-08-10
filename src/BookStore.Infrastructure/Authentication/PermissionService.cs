using BookStore.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Authentication;

/// <summary>
/// Provides authorization checks against the current session.
/// </summary>
public class PermissionService : IAuthorizationService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PermissionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionService"/> class.
    /// </summary>
    /// <param name="currentUserService">The current user service.</param>
    /// <param name="logger">The logger.</param>
    public PermissionService(ICurrentUserService currentUserService, ILogger<PermissionService> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool HasPermission(string permission)
    {
        var authorized = _currentUserService.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        if (!authorized)
        {
            _logger.LogWarning("Unauthorized permission check: {Permission}", permission);
        }

        return authorized;
    }

    /// <inheritdoc />
    public bool HasPermissions(params string[] permissions)
    {
        return permissions.All(HasPermission);
    }

    /// <inheritdoc />
    public bool HasRole(string role)
    {
        var authorized = string.Equals(_currentUserService.Role, role, StringComparison.OrdinalIgnoreCase);
        if (!authorized)
        {
            _logger.LogWarning("Unauthorized role check: {Role}", role);
        }

        return authorized;
    }

    /// <inheritdoc />
    public bool CanAccess(string requiredPermission)
    {
        return HasPermission(requiredPermission);
    }
}
