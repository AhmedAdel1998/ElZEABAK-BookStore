namespace BookStore.Application.Features.Categories.Commands.ActivateCategory;

/// <summary>
/// Requests activation of a category.
/// </summary>
/// <param name="Id">The category identifier.</param>
public sealed record ActivateCategoryRequest(Guid Id);
