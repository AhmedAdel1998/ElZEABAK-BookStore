namespace BookStore.Application.Features.Categories.DTOs;

/// <summary>
/// Represents editable category form data.
/// </summary>
public sealed class CategoryEditorModel
{
    /// <summary>Gets or sets the category identifier when editing.</summary>
    public Guid? Id { get; set; }

    /// <summary>Gets or sets the category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the category description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a value indicating whether the category is active.</summary>
    public bool IsActive { get; set; } = true;
}
