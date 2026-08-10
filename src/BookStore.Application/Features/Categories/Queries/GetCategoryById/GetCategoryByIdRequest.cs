namespace BookStore.Application.Features.Categories.Queries.GetCategoryById;

/// <summary>
/// Requests a category by identifier.
/// </summary>
/// <param name="Id">The category identifier.</param>
public sealed record GetCategoryByIdRequest(Guid Id);
