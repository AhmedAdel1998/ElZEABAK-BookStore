using System.Windows.Controls;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Views.Authentication;

/// <summary>
/// Interaction logic for the change password view.
/// </summary>
public partial class ChangePasswordView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordView"/> class.
    /// </summary>
    public ChangePasswordView()
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
