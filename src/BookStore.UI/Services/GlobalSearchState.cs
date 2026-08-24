namespace BookStore.UI.Services;

/// <summary>
/// Default global search state.
/// </summary>
public sealed class GlobalSearchState : IGlobalSearchState
{
    private string? _pendingFilter;

    /// <inheritdoc />
    public void SetPendingFilter(string term) =>
        _pendingFilter = string.IsNullOrWhiteSpace(term) ? null : term.Trim();

    /// <inheritdoc />
    public string? ConsumePendingFilter()
    {
        var term = _pendingFilter;
        _pendingFilter = null;
        return term;
    }
}
