namespace BookStore.Application.Features.Categories.DTOs;

/// <summary>
/// Represents category data displayed in list and details screens.
/// </summary>
public sealed class CategoryDto
{
    /// <summary>Gets or sets the category identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the category description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a value indicating whether the category is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the number of products assigned to the category.</summary>
    public int ProductCount { get; set; }

    /// <summary>Gets or sets the created date.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the updated date.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets the display status.</summary>
    public string Status => IsActive ? "Active" : "Inactive";
}
