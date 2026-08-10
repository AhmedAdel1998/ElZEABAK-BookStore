using System.Windows;

namespace BookStore.UI.Services;

/// <summary>
/// WPF implementation of application shutdown.
/// </summary>
public class ApplicationShutdownService : IApplicationShutdownService
{
    /// <inheritdoc />
    public void Shutdown()
    {
        System.Windows.Application.Current.Shutdown();
    }
}
