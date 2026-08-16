using System.Windows.Controls;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Views.Login;

public partial class FirstRunSetupView : UserControl
{
    public FirstRunSetupView()
    {
        InitializeComponent();
    }

    private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is FirstRunSetupViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void OnConfirmPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is FirstRunSetupViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ConfirmPassword = passwordBox.Password;
        }
    }
}
