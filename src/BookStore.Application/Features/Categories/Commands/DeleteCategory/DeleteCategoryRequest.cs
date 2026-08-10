namespace BookStore.Application.Features.Categories.Commands.DeleteCategory;

/// <summary>
/// Requests soft deletion of a category.
/// </summary>
/// <param name="Id">The category identifier.</param>
public sealed record DeleteCategoryRequest(Guid Id);
