namespace BookStore.Application.Features.Categories.Commands.UpdateCategory;

/// <summary>
/// Requests an update to a category.
/// </summary>
/// <param name="Id">The category identifier.</param>
/// <param name="Name">The category name.</param>
/// <param name="Description">The optional description.</param>
/// <param name="IsActive">Whether the category should be active.</param>
public sealed record UpdateCategoryRequest(Guid Id, string Name, string? Description, bool IsActive);
