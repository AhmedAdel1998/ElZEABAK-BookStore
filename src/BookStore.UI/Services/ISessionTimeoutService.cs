namespace BookStore.UI.Services;

/// <summary>
/// Tracks user inactivity and expires authenticated sessions.
/// </summary>
public interface ISessionTimeoutService
{
    /// <summary>
    /// Starts inactivity tracking.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops inactivity tracking.
    /// </summary>
    void Stop();
}
