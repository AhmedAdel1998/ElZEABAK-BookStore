using CommunityToolkit.Mvvm.ComponentModel;
using BookStore.UI.Services;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// Maintains navigation state for the authenticated shell content region.
/// </summary>
public partial class ShellNavigationService : ObservableObject, IShellNavigationService
{
    private const string BreadcrumbSeparator = " > ";

    private readonly IViewModelFactory _viewModelFactory;
    private readonly ILocalizationService _localizationService;
    private readonly Stack<(Type Type, string Breadcrumb)> _backStack = [];
    private readonly Stack<(Type Type, string Breadcrumb)> _forwardStack = [];
    private Type? _currentType;
    private string _breadcrumbSource = "Dashboard";
    private IViewModelLease? _currentLease;

    [ObservableProperty]
    private BaseViewModel? currentViewModel;

    [ObservableProperty]
    private string breadcrumb = "Dashboard";

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellNavigationService"/> class.
    /// </summary>
    /// <param name="viewModelFactory">The view model factory.</param>
    /// <param name="localizationService">Supplies breadcrumb translations.</param>
    public ShellNavigationService(IViewModelFactory viewModelFactory, ILocalizationService localizationService)
    {
        _viewModelFactory = viewModelFactory;
        _localizationService = localizationService;
        _localizationService.CultureChanged += (_, _) => Breadcrumb = Localize(_breadcrumbSource);
        Breadcrumb = Localize(_breadcrumbSource);
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

        _forwardStack.Push((_currentType, _breadcrumbSource));
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

        _backStack.Push((_currentType, _breadcrumbSource));
        var target = _forwardStack.Pop();
        NavigateTo(target.Type, target.Breadcrumb, addToHistory: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RefreshAsync()
    {
        if (_currentType is not null)
        {
            NavigateTo(_currentType, _breadcrumbSource, addToHistory: false);
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
            _backStack.Push((_currentType, _breadcrumbSource));
            _forwardStack.Clear();
        }

        var lease = _viewModelFactory.Create(type);

        // Release the page being replaced only once its successor exists, so a construction
        // failure leaves the current screen intact rather than blanking the content region.
        var previous = _currentLease;
        _currentLease = lease;
        CurrentViewModel = lease.ViewModel;
        _breadcrumbSource = breadcrumb;
        Breadcrumb = Localize(breadcrumb);
        _currentType = type;
        previous?.Dispose();
    }

    /// <summary>
    /// Translates a breadcrumb trail one segment at a time. Callers author trails such as
    /// "Reports &gt; Sales Summary"; the whole trail is never a dictionary entry, but each
    /// segment is, so the untranslated source is kept and re-localized whenever the language
    /// changes.
    /// </summary>
    private string Localize(string breadcrumb)
    {
        if (string.IsNullOrWhiteSpace(breadcrumb))
        {
            return breadcrumb;
        }

        var segments = breadcrumb.Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0
            ? breadcrumb
            : string.Join(BreadcrumbSeparator, segments.Select(_localizationService.TranslateLiteral));
    }
}
