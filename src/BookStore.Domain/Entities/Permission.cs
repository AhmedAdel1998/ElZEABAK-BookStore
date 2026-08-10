using BookStore.Domain.Common;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents an application permission.
/// </summary>
public class Permission : BaseEntity
{
    private Permission()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Permission"/> class.
    /// </summary>
    /// <param name="name">The permission name.</param>
    /// <param name="description">The permission description.</param>
    public Permission(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Permission name is required.");
        }

        Name = name.Trim();
        Description = description;
    }

    /// <summary>
    /// Gets the permission name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the permission description.
    /// </summary>
    public string? Description { get; private set; }
}
