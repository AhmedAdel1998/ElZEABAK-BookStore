using System.Windows;
using System.Windows.Controls;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Views.Authentication;

/// <summary>
/// Interaction logic for the authenticated home view.
/// </summary>
public partial class AuthenticatedHomeView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatedHomeView"/> class.
    /// </summary>
    public AuthenticatedHomeView()
    {
        InitializeComponent();

        // WPF has no adaptive layout triggers, so the one place that knows the shell's real width is
        // the shell itself. Both events are needed: SizeChanged does not fire for a window that
        // opens already at its final size.
        Loaded += OnShellSizeChanged;
        SizeChanged += OnShellSizeChanged;
    }

    private void OnShellSizeChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AuthenticatedHomeViewModel viewModel)
        {
            viewModel.ApplyAvailableWidth(ActualWidth);
        }
    }
}
