using BookStore.Domain.Common;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents a product category.
/// </summary>
public class Category : BaseEntity, IAggregateRoot
{
    private readonly List<Product> _products = [];

    private Category()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Category"/> class.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="description">The category description.</param>
    public Category(string name, string? description = null)
    {
        SetName(name);
        Description = description;
        IsActive = true;
    }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the category description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the category is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the products assigned to the category.
    /// </summary>
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    /// <summary>
    /// Renames the category.
    /// </summary>
    /// <param name="name">The new category name.</param>
    public void Rename(string name)
    {
        SetName(name);
        MarkUpdated();
    }

    /// <summary>
    /// Updates the category description.
    /// </summary>
    /// <param name="description">The category description.</param>
    public void UpdateDescription(string? description)
    {
        Description = description;
        MarkUpdated();
    }

    /// <summary>
    /// Activates the category.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    /// <summary>
    /// Deactivates the category.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Category name is required.");
        }

        if (name.Length > 100)
        {
            throw new ValidationException("Category name cannot exceed 100 characters.");
        }

        Name = name.Trim();
    }
}
