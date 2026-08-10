namespace BookStore.Application.Features.Categories.Commands.CreateCategory;

/// <summary>
/// Requests creation of a category.
/// </summary>
/// <param name="Name">The category name.</param>
/// <param name="Description">The optional description.</param>
/// <param name="IsActive">Whether the new category should be active.</param>
public sealed record CreateCategoryRequest(string Name, string? Description, bool IsActive = true);
