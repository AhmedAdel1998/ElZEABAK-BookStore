using CommunityToolkit.Mvvm.ComponentModel;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// Maintains navigation state for the authenticated shell content region.
/// </summary>
public partial class ShellNavigationService : ObservableObject, IShellNavigationService
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly Stack<(Type Type, string Breadcrumb)> _backStack = [];
    private readonly Stack<(Type Type, string Breadcrumb)> _forwardStack = [];
    private Type? _currentType;

    [ObservableProperty]
    private BaseViewModel? currentViewModel;

    [ObservableProperty]
    private string breadcrumb = "Dashboard";

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellNavigationService"/> class.
    /// </summary>
    /// <param name="viewModelFactory">The view model factory.</param>
    public ShellNavigationService(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    /// <inheritdoc />
    public Task NavigateToAsync<TViewModel>()
        where TViewModel : BaseViewModel
    {
        return NavigateToAsync<TViewModel>(typeof(TViewModel).Name.Replace("ViewModel", string.Empty, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public Task NavigateToAsync<TViewModel>(string breadcrumb)
        where TViewModel : BaseViewModel
    {
        NavigateTo(typeof(TViewModel), breadcrumb, addToHistory: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        if (_backStack.Count == 0 || _currentType is null)
        {
            return Task.CompletedTask;
        }

        _forwardStack.Push((_currentType, Breadcrumb));
        var target = _backStack.Pop();
        NavigateTo(target.Type, target.Breadcrumb, addToHistory: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GoForwardAsync()
    {
        if (_forwardStack.Count == 0 || _currentType is null)
        {
            return Task.CompletedTask;
        }

        _backStack.Push((_currentType, Breadcrumb));
        var target = _forwardStack.Pop();
        NavigateTo(target.Type, target.Breadcrumb, addToHistory: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RefreshAsync()
    {
        if (_currentType is not null)
        {
            NavigateTo(_currentType, Breadcrumb, addToHistory: false);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        _backStack.Clear();
        _forwardStack.Clear();
    }

    private void NavigateTo(Type type, string breadcrumb, bool addToHistory)
    {
        if (addToHistory && _currentType is not null && _currentType != type)
        {
            _backStack.Push((_currentType, Breadcrumb));
            _forwardStack.Clear();
        }

        if (CurrentViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        CurrentViewModel = _viewModelFactory.Create(type);
        Breadcrumb = breadcrumb;
        _currentType = type;
    }
}
