using CommunityToolkit.Mvvm.ComponentModel;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// Maintains shell navigation state.
/// </summary>
public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly Stack<Type> _backStack = [];
    private readonly Stack<Type> _forwardStack = [];
    private Type? _currentViewModelType;

    [ObservableProperty]
    private BaseViewModel? currentViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="viewModelFactory">The view model factory.</param>
    public NavigationService(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    /// <inheritdoc />
    public Task NavigateToAsync<TViewModel>()
        where TViewModel : BaseViewModel
    {
        NavigateTo(typeof(TViewModel), addToHistory: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        if (_backStack.Count == 0 || _currentViewModelType is null)
        {
            return Task.CompletedTask;
        }

        _forwardStack.Push(_currentViewModelType);
        NavigateTo(_backStack.Pop(), addToHistory: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GoForwardAsync()
    {
        if (_forwardStack.Count == 0 || _currentViewModelType is null)
        {
            return Task.CompletedTask;
        }

        _backStack.Push(_currentViewModelType);
        NavigateTo(_forwardStack.Pop(), addToHistory: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RefreshAsync()
    {
        if (_currentViewModelType is not null)
        {
            NavigateTo(_currentViewModelType, addToHistory: false);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        _backStack.Clear();
        _forwardStack.Clear();
    }

    private void NavigateTo(Type viewModelType, bool addToHistory)
    {
        if (addToHistory && _currentViewModelType is not null && _currentViewModelType != viewModelType)
        {
            _backStack.Push(_currentViewModelType);
            _forwardStack.Clear();
        }

        if (CurrentViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        CurrentViewModel = _viewModelFactory.Create(viewModelType);
        _currentViewModelType = viewModelType;
    }
}
