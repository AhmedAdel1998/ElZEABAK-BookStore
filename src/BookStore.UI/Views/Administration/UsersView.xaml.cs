using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Views.Administration;

/// <summary>Interaction logic for user administration.</summary>
public partial class UsersView : UserControl
{
    private UsersViewModel? _viewModel;

    /// <summary>Initializes the user administration view.</summary>
    public UsersView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Subscribe(null);
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel viewModel && sender is PasswordBox passwordBox) viewModel.NewPassword = passwordBox.Password;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => Subscribe(e.NewValue as UsersViewModel);

    private void Subscribe(UsersViewModel? viewModel)
    {
        if (_viewModel is not null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = viewModel;
        if (_viewModel is not null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UsersViewModel.NewPassword) && string.IsNullOrEmpty(_viewModel?.NewPassword) && PasswordInput.Password.Length > 0) PasswordInput.Clear();
    }
}
