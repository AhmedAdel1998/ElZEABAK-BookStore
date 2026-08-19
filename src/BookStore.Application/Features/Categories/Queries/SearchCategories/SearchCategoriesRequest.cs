namespace BookStore.Application.Features.Categories.Queries.SearchCategories;

/// <summary>
/// Requests paged category search results.
/// </summary>
/// <param name="SearchTerm">The optional search term.</param>
/// <param name="PageNumber">The page number.</param>
/// <param name="PageSize">The page size.</param>
public sealed record SearchCategoriesRequest(string? SearchTerm, int PageNumber = 1, int PageSize = 25, bool? IsActive = null);
