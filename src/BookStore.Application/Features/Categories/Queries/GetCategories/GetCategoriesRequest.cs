namespace BookStore.Application.Features.Categories.Queries.GetCategories;

/// <summary>
/// Requests paged categories.
/// </summary>
/// <param name="PageNumber">The page number.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetCategoriesRequest(int PageNumber = 1, int PageSize = 25);
