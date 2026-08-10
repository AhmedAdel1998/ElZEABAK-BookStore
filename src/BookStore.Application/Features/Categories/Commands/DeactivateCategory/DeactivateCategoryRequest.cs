namespace BookStore.Application.Features.Categories.Commands.DeactivateCategory;

/// <summary>
/// Requests deactivation of a category.
/// </summary>
/// <param name="Id">The category identifier.</param>
public sealed record DeactivateCategoryRequest(Guid Id);
