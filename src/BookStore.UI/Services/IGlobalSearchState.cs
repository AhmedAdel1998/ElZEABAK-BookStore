namespace BookStore.UI.Services;

/// <summary>
/// Carries a search term from the shell's global search box to the list page it opens.
/// </summary>
/// <remarks>
/// Shell navigation is view-model-first and builds each page in its own dependency injection
/// scope, so there is no way to pass it an argument. The term is parked here, consumed once by
/// the page that opens next, and cleared, so that reaching the same page later by hand starts
/// unfiltered. This mirrors <see cref="IProductNavigationState"/>.
/// </remarks>
public interface IGlobalSearchState
{
    /// <summary>Stores the term the next list page should filter by.</summary>
    /// <param name="term">The search term.</param>
    void SetPendingFilter(string term);

    /// <summary>Returns the pending term and clears it.</summary>
    /// <returns>The pending term, or <see langword="null"/> when nothing is pending.</returns>
    string? ConsumePendingFilter();
}
