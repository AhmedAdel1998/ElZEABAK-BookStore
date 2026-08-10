using BookStore.Domain.Common;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents an application role.
/// </summary>
public class Role : BaseEntity, IAggregateRoot
{
    private readonly List<Permission> _permissions = [];

    private Role()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="description">The role description.</param>
    public Role(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Role name is required.");
        }

        Name = name.Trim();
        Description = description;
    }

    /// <summary>
    /// Gets the role name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the role description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets role permissions.
    /// </summary>
    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();

    /// <summary>
    /// Adds a permission to the role.
    /// </summary>
    /// <param name="permission">The permission to add.</param>
    public void AddPermission(Permission permission)
    {
        if (_permissions.Any(existing => existing.Id == permission.Id))
        {
            return;
        }

        _permissions.Add(permission);
        MarkUpdated();
    }
}
