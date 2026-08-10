namespace BookStore.Application.Interfaces;

/// <summary>
/// Provides role and permission checks.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Determines whether the current user has a permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns><see langword="true"/> when authorized; otherwise, <see langword="false"/>.</returns>
    bool HasPermission(string permission);

    /// <summary>
    /// Determines whether the current user has every supplied permission.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns><see langword="true"/> when authorized; otherwise, <see langword="false"/>.</returns>
    bool HasPermissions(params string[] permissions);

    /// <summary>
    /// Determines whether the current user has a role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns><see langword="true"/> when authorized; otherwise, <see langword="false"/>.</returns>
    bool HasRole(string role);

    /// <summary>
    /// Determines whether the current user can access a feature.
    /// </summary>
    /// <param name="requiredPermission">The required permission.</param>
    /// <returns><see langword="true"/> when access is allowed; otherwise, <see langword="false"/>.</returns>
    bool CanAccess(string requiredPermission);
}
