using System.Windows;
using BookStore.UI.Services;
using BookStore.UI.ViewModels;

namespace BookStore.UI;

/// <summary>
/// Main application shell.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The shell view model.</param>
    public MainWindow(MainViewModel viewModel, IUiTreeLocalizer uiTreeLocalizer)
    {
        InitializeComponent();
        DataContext = viewModel;
        uiTreeLocalizer.Attach(this);
    }
}
