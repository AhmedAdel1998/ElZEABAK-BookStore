using System.Windows.Controls;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Views.Shell;

/// <summary>
/// Change password region view hosted inside the application shell.
/// </summary>
public partial class ChangePasswordRegionView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordRegionView"/> class.
    /// </summary>
    public ChangePasswordRegionView()
    {
        InitializeComponent();
    }

    private void OnCurrentPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.CurrentPassword = passwordBox.Password;
        }
    }

    private void OnNewPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.NewPassword = passwordBox.Password;
        }
    }

    private void OnConfirmPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ConfirmPassword = passwordBox.Password;
        }
    }
}
