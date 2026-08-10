namespace BookStore.Shared.Results;

/// <summary>
/// Represents a paged set of items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets the current page items.
    /// </summary>
    public IReadOnlyCollection<T> Items { get; init; } = [];

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Gets the number of items requested per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total item count.
    /// </summary>
    public int TotalCount { get; init; }
}
