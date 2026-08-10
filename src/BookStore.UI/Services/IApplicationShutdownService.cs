namespace BookStore.UI.Services;

/// <summary>
/// Provides application shutdown behavior.
/// </summary>
public interface IApplicationShutdownService
{
    /// <summary>
    /// Shuts down the application.
    /// </summary>
    void Shutdown();
}
