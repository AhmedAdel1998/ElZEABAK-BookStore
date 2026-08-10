using System.Windows;

namespace BookStore.UI.Services;

/// <summary>
/// WPF shell window state service.
/// </summary>
public class WindowStateService : IWindowStateService, IWindowService
{
    /// <inheritdoc />
    public void Minimize()
    {
        System.Windows.Application.Current.MainWindow.WindowState = WindowState.Minimized;
    }

    /// <inheritdoc />
    public void ToggleMaximize()
    {
        var window = System.Windows.Application.Current.MainWindow;
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    /// <inheritdoc />
    public void Close()
    {
        System.Windows.Application.Current.Shutdown();
    }
}
