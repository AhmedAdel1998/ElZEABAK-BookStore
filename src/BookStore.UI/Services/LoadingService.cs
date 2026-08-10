namespace BookStore.UI.Services;

/// <summary>
/// Default loading overlay service.
/// </summary>
public class LoadingService : ILoadingService
{
    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public bool IsLoading { get; private set; }

    /// <inheritdoc />
    public string LoadingText { get; private set; } = "Loading...";

    /// <inheritdoc />
    public void Show(string text)
    {
        LoadingText = text;
        IsLoading = true;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Hide()
    {
        IsLoading = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
