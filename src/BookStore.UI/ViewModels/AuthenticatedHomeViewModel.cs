using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Customers.Queries.SearchCustomers;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Handlers;
using BookStore.Application.Features.Products.Queries.SearchProducts;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Threading;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Authenticated application shell view model.
/// </summary>
public partial class AuthenticatedHomeViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigationService _applicationNavigationService;
    private readonly IShellNavigationService _shellNavigationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISessionTimeoutService _sessionTimeoutService;
    private readonly INotificationService _notificationService;
    private readonly ILoadingService _loadingService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProductNavigationState _productNavigationState;
    private readonly ICustomerNavigationState _customerNavigationState;
    private readonly IGlobalSearchState _globalSearchState;
    private readonly DispatcherTimer _clockTimer;

    /// <summary>Cancels the in-flight global search once a newer keystroke supersedes it.</summary>
    private CancellationTokenSource? _searchCancellation;

    [ObservableProperty]
    private bool isSidebarCollapsed;

    /// <summary>
    /// The collapse state the cashier chose by hand, remembered across automatic collapses so that
    /// widening the window restores what they actually wanted.
    /// </summary>
    private bool _sidebarCollapsedByUser;

    /// <summary>Whether the current collapse was forced by the window being too narrow.</summary>
    private bool _sidebarCollapsedByWidth;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isSearchResultsOpen;

    [ObservableProperty]
    private string searchStatus = string.Empty;

    [ObservableProperty]
    private string currentTime = string.Empty;

    [ObservableProperty]
    private string databaseStatus = string.Empty;

    [ObservableProperty]
    private string internetStatus = string.Empty;

    [ObservableProperty]
    private int pendingBackgroundTasks;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string loadingText = string.Empty;

    [ObservableProperty]
    private string languageToggleText = string.Empty;

    [ObservableProperty]
    private int sidebarColumn;

    [ObservableProperty]
    private int contentColumn = 1;

    [ObservableProperty]
    private GridLength firstColumnWidth = GridLength.Auto;

    [ObservableProperty]
    private GridLength secondColumnWidth = new(1, GridUnitType.Star);

    [ObservableProperty]
    private bool isNotificationsOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatedHomeViewModel"/> class.
    /// </summary>
    public AuthenticatedHomeViewModel(
        IAuthenticationService authenticationService,
        INavigationService applicationNavigationService,
        IShellNavigationService shellNavigationService,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ISessionTimeoutService sessionTimeoutService,
        INotificationService notificationService,
        ILoadingService loadingService,
        IThemeService themeService,
        ILocalizationService localizationService,
        IServiceScopeFactory scopeFactory,
        IProductNavigationState productNavigationState,
        ICustomerNavigationState customerNavigationState,
        IGlobalSearchState globalSearchState)
    {
        _authenticationService = authenticationService;
        _applicationNavigationService = applicationNavigationService;
        _shellNavigationService = shellNavigationService;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _sessionTimeoutService = sessionTimeoutService;
        _notificationService = notificationService;
        _loadingService = loadingService;
        _themeService = themeService;
        _localizationService = localizationService;
        _scopeFactory = scopeFactory;
        _productNavigationState = productNavigationState;
        _customerNavigationState = customerNavigationState;
        _globalSearchState = globalSearchState;
        _loadingService.StateChanged += OnLoadingStateChanged;
        _localizationService.CultureChanged += OnCultureChanged;
        _notificationService.Notifications.CollectionChanged += OnNotificationsChanged;
        ApplyLocalizedText();
        BuildNavigationItems();
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _clockTimer.Start();
        _ = NavigateDashboardAsync();
    }

    /// <summary>
    /// Gets the shell navigation service.
    /// </summary>
    public IShellNavigationService ShellNavigation => _shellNavigationService;

    /// <summary>
    /// Gets menu items available to the current user.
    /// </summary>
    public ObservableCollection<NavigationItem> MenuItems { get; } = [];

    /// <summary>
    /// Gets the matches for the text currently in the global search box.
    /// </summary>
    public ObservableCollection<GlobalSearchResult> SearchResults { get; } = [];

    /// <summary>
    /// Gets active notifications.
    /// </summary>
    public ObservableCollection<NotificationMessage> Notifications => _notificationService.Notifications;

    /// <summary>
    /// Gets active notification count.
    /// </summary>
    public int NotificationCount => Notifications.Count;

    /// <summary>
    /// Gets the signed-in user's display name.
    /// </summary>
    public string DisplayName => _currentUserService.FullName ?? _currentUserService.Username ?? "User";

    /// <summary>
    /// Gets the signed-in user's role.
    /// </summary>
    public string Role => _currentUserService.Role ?? string.Empty;

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string Version => "v1.0";

    /// <summary>
    /// Shortest term the global search will run. One character matches most of the catalogue and
    /// makes the dropdown useless.
    /// </summary>
    private const int MinimumSearchLength = 2;

    /// <summary>Most product rows the dropdown shows.</summary>
    private const int MaximumProductResults = 5;

    /// <summary>Most customer rows the dropdown shows.</summary>
    private const int MaximumCustomerResults = 3;

    /// <summary>Most page rows the dropdown shows.</summary>
    private const int MaximumPageResults = 3;

    /// <summary>
    /// How long typing must pause before the search reaches the database. Debouncing lives here
    /// rather than in the binding so that <see cref="SearchText"/> always holds what has actually
    /// been typed -- Enter acts on the whole term even when it follows the last keystroke at once.
    /// </summary>
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Re-runs the global search whenever the box changes.
    /// </summary>
    partial void OnSearchTextChanged(string value) => _ = RunGlobalSearchAsync(value);

    /// <summary>
    /// Opens the result the cashier clicked.
    /// </summary>
    [RelayCommand]
    private async Task OpenSearchResultAsync(GlobalSearchResult? result)
    {
        if (result is null)
        {
            return;
        }

        // Emptying the box also closes the dropdown and clears the rows, so the shell is back to
        // its resting state by the time the destination page appears.
        SearchText = string.Empty;
        await result.Open();
    }

    /// <summary>
    /// Opens the first match, which is what Enter in the search box does. With no matches at all
    /// the term is handed to the product list, so a search that finds nothing still lands
    /// somewhere the cashier can widen it.
    /// </summary>
    [RelayCommand]
    private async Task OpenTopSearchResultAsync()
    {
        if (SearchResults.Count > 0)
        {
            await OpenSearchResultAsync(SearchResults[0]);
            return;
        }

        var term = SearchText.Trim();
        if (term.Length < MinimumSearchLength || !_authorizationService.HasPermission(PermissionConstants.ProductView))
        {
            return;
        }

        _globalSearchState.SetPendingFilter(term);
        SearchText = string.Empty;
        await NavigateProductsAsync();
    }

    /// <summary>
    /// Closes the results dropdown, keeping the typed term so it can be edited.
    /// </summary>
    [RelayCommand]
    private void CloseSearchResults()
    {
        SearchResults.Clear();
        SearchStatus = string.Empty;
        IsSearchResultsOpen = false;
    }

    private async Task RunGlobalSearchAsync(string text)
    {
        // Whatever the previous keystroke started is stale now. Cancelling it also stops its
        // results from arriving after this run's and overwriting them. Only the run that owns a
        // source disposes it, so cancelling here cannot pull the token out from under a query
        // that is still reading it.
        var previous = _searchCancellation;
        _searchCancellation = null;
        previous?.Cancel();

        var term = text.Trim();
        if (term.Length < MinimumSearchLength)
        {
            CloseSearchResults();
            return;
        }

        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        try
        {
            await Task.Delay(SearchDebounce, cancellation.Token);
            var matches = await FindMatchesAsync(term, cancellation.Token);
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            SearchResults.Clear();
            foreach (var match in matches)
            {
                SearchResults.Add(match);
            }

            SearchStatus = matches.Count == 0 ? _localizationService.T("Search.NoMatches") : string.Empty;
            IsSearchResultsOpen = true;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer term.
        }
        catch (Exception)
        {
            SearchResults.Clear();
            SearchStatus = _localizationService.T("Search.Failed");
            IsSearchResultsOpen = true;
        }
        finally
        {
            if (ReferenceEquals(_searchCancellation, cancellation))
            {
                _searchCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    /// <summary>
    /// Collects the pages, products and customers matching a term.
    /// </summary>
    /// <remarks>
    /// The shell outlives every page, so it must not hold a database context of its own: a context
    /// kept for the whole session would be shared by overlapping searches and throw "a second
    /// operation was started on this context". Each search takes a scope of its own instead, which
    /// is the same guarantee <see cref="ViewModelFactory"/> gives a page.
    /// </remarks>
    private async Task<List<GlobalSearchResult>> FindMatchesAsync(string term, CancellationToken cancellationToken)
    {
        var matches = new List<GlobalSearchResult>();
        var pageGroup = _localizationService.T("Search.Pages");
        // Logout is deliberately not reachable from here: signing out is not something to trigger
        // by typing three letters and pressing Enter.
        foreach (var item in MenuItems
            .Where(menuItem => menuItem.Command is not null
                && !string.Equals(menuItem.TextKey, "Nav.Logout", StringComparison.Ordinal)
                && menuItem.Text.Contains(term, StringComparison.CurrentCultureIgnoreCase))
            .Take(MaximumPageResults))
        {
            var command = item.Command!;
            matches.Add(new GlobalSearchResult
            {
                Group = pageGroup,
                PrimaryText = item.Text,
                Open = () =>
                {
                    if (command.CanExecute(null))
                    {
                        command.Execute(null);
                    }

                    return Task.CompletedTask;
                }
            });
        }

        using var scope = _scopeFactory.CreateScope();

        if (_authorizationService.HasPermission(PermissionConstants.ProductView))
        {
            var productHandler = scope.ServiceProvider.GetRequiredService<SearchProductsHandler>();
            var products = await productHandler.HandleAsync(
                new SearchProductsRequest(new ProductFilter { SearchTerm = term, PageSize = MaximumProductResults }),
                cancellationToken);
            if (products.IsSuccess && products.Value is not null)
            {
                var productGroup = _localizationService.T("Search.Products");
                foreach (var product in products.Value.Items)
                {
                    var productId = product.Id;
                    matches.Add(new GlobalSearchResult
                    {
                        Group = productGroup,
                        PrimaryText = product.Title,
                        SecondaryText = product.Barcode,
                        Open = () =>
                        {
                            _productNavigationState.SelectedProductId = productId;
                            return OpenPageAsync<ProductDetailsViewModel>("Products > Details");
                        }
                    });
                }
            }
        }

        if (_authorizationService.HasPermission(PermissionConstants.CustomerView))
        {
            var customerHandler = scope.ServiceProvider.GetRequiredService<SearchCustomersHandler>();
            var customers = await customerHandler.HandleAsync(
                new SearchCustomersRequest(new CustomerFilter { SearchTerm = term, PageSize = MaximumCustomerResults }),
                cancellationToken);
            if (customers.IsSuccess && customers.Value is not null)
            {
                var customerGroup = _localizationService.T("Search.Customers");
                foreach (var customer in customers.Value.Items.Where(candidate => candidate.Id.HasValue))
                {
                    var customerId = customer.Id!.Value;
                    matches.Add(new GlobalSearchResult
                    {
                        Group = customerGroup,
                        PrimaryText = customer.FullName,
                        SecondaryText = customer.Phone,
                        Open = () =>
                        {
                            _customerNavigationState.SelectedCustomerId = customerId;
                            return OpenPageAsync<CustomerDetailsViewModel>("Customers > Details");
                        }
                    });
                }
            }
        }

        return matches;
    }

    private async Task OpenPageAsync<TViewModel>(string breadcrumb)
        where TViewModel : BaseViewModel
    {
        _loadingService.Show(_localizationService.T("Loading.OpeningPage"));
        try
        {
            await _shellNavigationService.NavigateToAsync<TViewModel>(breadcrumb);
        }
        finally
        {
            _loadingService.Hide();
        }
    }

    /// <summary>
    /// Toggles sidebar collapsed state.
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        _sidebarCollapsedByUser = IsSidebarCollapsed;
        _sidebarCollapsedByWidth = false;
    }

    /// <summary>
    /// Reacts to the shell being resized by collapsing the sidebar once the window is too narrow to
    /// hold both it and a working page.
    /// </summary>
    /// <remarks>
    /// The expanded rail is 272px. At the window minimum that left the POS screen roughly 690px for
    /// three columns that need more than 800, so the payment panel -- and with it Complete Sale --
    /// was pushed off screen and the sale could not be finished. Collapsing to the 76px icon rail
    /// gives the page back the space it needs. A manual choice made while wide is restored when the
    /// window grows again.
    /// </remarks>
    /// <param name="availableWidth">The shell's current width in device-independent pixels.</param>
    public void ApplyAvailableWidth(double availableWidth)
    {
        if (double.IsNaN(availableWidth) || availableWidth <= 0)
        {
            return;
        }

        if (availableWidth < SidebarCollapseWidth)
        {
            if (!IsSidebarCollapsed)
            {
                _sidebarCollapsedByWidth = true;
                IsSidebarCollapsed = true;
            }

            return;
        }

        if (_sidebarCollapsedByWidth)
        {
            _sidebarCollapsedByWidth = false;
            IsSidebarCollapsed = _sidebarCollapsedByUser;
        }
    }

    /// <summary>
    /// Width below which the sidebar collapses to its icon rail. Set from the widest page minimum
    /// (the POS screen) plus the expanded rail.
    /// </summary>
    private const double SidebarCollapseWidth = 1120d;

    /// <summary>
    /// Toggles theme state for future theme persistence.
    /// </summary>
    [RelayCommand]
    private async Task ToggleThemeAsync()
    {
        await _themeService.ToggleThemeAsync();
        _notificationService.Show(_localizationService.T("Shell.Theme"), $"{_themeService.CurrentTheme} theme applied.", NotificationSeverity.Information);
    }

    [RelayCommand]
    private async Task ToggleLanguageAsync()
    {
        await _localizationService.ToggleLanguageAsync();
    }

    [RelayCommand]
    private void ToggleNotifications()
    {
        IsNotificationsOpen = !IsNotificationsOpen;
    }

    [RelayCommand]
    private void ClearNotifications()
    {
        _notificationService.Clear();
        IsNotificationsOpen = false;
    }

    /// <summary>
    /// Navigates back in shell history.
    /// </summary>
    [RelayCommand]
    private Task GoBackAsync() => _shellNavigationService.GoBackAsync();

    /// <summary>
    /// Navigates forward in shell history.
    /// </summary>
    [RelayCommand]
    private Task GoForwardAsync() => _shellNavigationService.GoForwardAsync();

    /// <summary>
    /// Refreshes the current shell page.
    /// </summary>
    [RelayCommand]
    private Task RefreshAsync() => _shellNavigationService.RefreshAsync();

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        _sessionTimeoutService.Stop();
        await _authenticationService.LogoutAsync();
        _shellNavigationService.ClearHistory();
        _applicationNavigationService.ClearHistory();
        await _applicationNavigationService.NavigateToAsync<LoginViewModel>();
    }

    /// <summary>
    /// Navigates to dashboard.
    /// </summary>
    [RelayCommand]
    private Task NavigateDashboardAsync() => NavigateAsync<DashboardViewModel>("Nav.Dashboard");

    /// <summary>
    /// Navigates to products.
    /// </summary>
    [RelayCommand]
    private Task NavigateProductsAsync() => NavigateAsync<ProductListViewModel>("Nav.Products");

    /// <summary>
    /// Navigates to inventory.
    /// </summary>
    [RelayCommand]
    private Task NavigateInventoryAsync() => NavigateAsync<InventoryDashboardViewModel>("Nav.Inventory");

    /// <summary>
    /// Navigates to barcode management.
    /// </summary>
    [RelayCommand]
    private Task NavigateBarcodeAsync() => NavigateAsync<BarcodePreviewViewModel>("Nav.Barcode");

    /// <summary>
    /// Navigates to sales.
    /// </summary>
    [RelayCommand]
    private Task NavigateSalesAsync() => NavigateAsync<POSViewModel>("Nav.SalesPOS");

    /// <summary>
    /// Navigates to reports.
    /// </summary>
    [RelayCommand]
    private Task NavigateReportsAsync() => NavigateAsync<ReportsDashboardViewModel>("Nav.Reports");

    /// <summary>
    /// Navigates to receipt reprinting.
    /// </summary>
    [RelayCommand]
    private Task NavigateReceiptsAsync() => NavigateAsync<ReprintReceiptViewModel>("Nav.Receipts");

    /// <summary>
    /// Navigates to backup.
    /// </summary>
    [RelayCommand]
    private Task NavigateBackupAsync() => NavigateAsync<BackupListViewModel>("Nav.Backup");

    /// <summary>
    /// Navigates to audit trail.
    /// </summary>
    [RelayCommand]
    private Task NavigateAuditAsync() => NavigateAsync<AuditTrailViewModel>("Nav.Audit");

    /// <summary>
    /// Navigates to data quality.
    /// </summary>
    [RelayCommand]
    private Task NavigateDataQualityAsync() => NavigateAsync<DataQualityViewModel>("Nav.DataQuality");

    /// <summary>
    /// Navigates to change password.
    /// </summary>
    [RelayCommand]
    private Task ChangePasswordAsync() => NavigateAsync<ChangePasswordViewModel>("Nav.Settings");

    private async Task NavigateAsync<TViewModel>(string breadcrumbKey)
        where TViewModel : BaseViewModel
    {
        _loadingService.Show(_localizationService.T("Loading.OpeningPage"));
        try
        {
            // The key travels, not its translation: the navigation service re-resolves the trail
            // every time the language changes, and a value translated here would arrive already
            // frozen in the current language.
            await _shellNavigationService.NavigateToAsync<TViewModel>(breadcrumbKey);
            foreach (var item in MenuItems)
            {
                item.IsActive = string.Equals(item.TextKey, breadcrumbKey, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            _loadingService.Hide();
        }
    }

    private void BuildNavigationItems()
    {
        AddMenuItem("Nav.Dashboard", "Dashboard", null, NavigateDashboardCommand);
        AddMenuItem("Nav.Categories", "Categories", PermissionConstants.CategoryView, new AsyncRelayCommand(() => NavigateAsync<CategoryListViewModel>("Nav.Categories")));
        AddMenuItem("Nav.Products", "Products", PermissionConstants.ProductView, NavigateProductsCommand);
        AddMenuItem("Nav.Inventory", "Inventory", PermissionConstants.InventoryView, NavigateInventoryCommand);
        AddMenuItem("Nav.Barcode", "Barcode", PermissionConstants.BarcodeView, NavigateBarcodeCommand);
        AddMenuItem("Nav.SalesPOS", "Sales", PermissionConstants.SalesCreate, NavigateSalesCommand);
        AddMenuItem("Nav.Customers", "Customers", PermissionConstants.CustomerView, new AsyncRelayCommand(() => NavigateAsync<CustomerListViewModel>("Nav.Customers")));
        AddMenuItem("Nav.Suppliers", "Suppliers", PermissionConstants.SupplierView, new AsyncRelayCommand(() => NavigateAsync<SupplierListViewModel>("Nav.Suppliers")));
        AddMenuItem("Nav.Reports", "Reports", PermissionConstants.ReportView, NavigateReportsCommand);
        AddMenuItem("Nav.Receipts", "Print", PermissionConstants.ReceiptReprint, NavigateReceiptsCommand);
        AddMenuItem("Nav.Settings", "Settings", PermissionConstants.SettingsView, new AsyncRelayCommand(() => NavigateAsync<SettingsViewModel>("Nav.Settings")));
        AddMenuItem("Nav.Users", "Users", PermissionConstants.UsersManage, new AsyncRelayCommand(() => NavigateAsync<UsersViewModel>("Nav.Users")));
        AddMenuItem("Nav.Roles", "Contact", PermissionConstants.RolesManage, new AsyncRelayCommand(() => NavigateAsync<RolesViewModel>("Nav.Roles")));
        AddMenuItem("Nav.Backup", "Backup", PermissionConstants.BackupView, NavigateBackupCommand);
        AddMenuItem("Nav.Audit", "History", PermissionConstants.AuditView, NavigateAuditCommand);
        AddMenuItem("Nav.DataQuality", "Validate", PermissionConstants.DataQualityView, NavigateDataQualityCommand);
        AddMenuItem("Nav.Logout", "Logout", null, LogoutCommand);
    }

    private void AddMenuItem(string textKey, string icon, string? permission, System.Windows.Input.ICommand command)
    {
        if (permission is not null && !_authorizationService.HasPermission(permission))
        {
            return;
        }

        MenuItems.Add(new NavigationItem
        {
            Text = _localizationService.T(textKey),
            Icon = icon,
            TextKey = textKey,
            RequiredPermission = permission,
            Command = command,
            Breadcrumb = _localizationService.T(textKey)
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _searchCancellation?.Cancel();
            _searchCancellation = null;
            _clockTimer.Stop();
        }

        base.Dispose(disposing);
    }

    private void OnLoadingStateChanged(object? sender, EventArgs e)
    {
        IsLoading = _loadingService.IsLoading;
        LoadingText = _loadingService.LoadingText;
    }

    private void OnNotificationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(NotificationCount));
    }

    private void ApplyLocalizedText()
    {
        Title = _localizationService.T("App.Title");
        DatabaseStatus = _localizationService.T("Status.Connected");
        InternetStatus = _localizationService.T("Status.Offline");
        LoadingText = _localizationService.T("Loading.Default");
        UpdateLanguageToggleText();
        UpdateShellColumns();
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        ApplyLocalizedText();
        foreach (var item in MenuItems)
        {
            item.Text = _localizationService.T(item.TextKey);
            item.Breadcrumb = item.Text;
        }
    }

    private void UpdateLanguageToggleText()
    {
        LanguageToggleText = _localizationService.IsRightToLeft
            ? _localizationService.T("Language.SwitchToEnglish")
            : _localizationService.T("Language.SwitchToArabic");
    }

    /// <summary>
    /// Moves the sidebar to the trailing edge for right-to-left layouts. The column widths move
    /// with it: the sidebar is fixed width and the content region takes the remaining space, so
    /// swapping only the column indexes would leave the sidebar in the star column and squeeze
    /// the content down to its own desired width.
    /// </summary>
    private void UpdateShellColumns()
    {
        var rightToLeft = _localizationService.IsRightToLeft;
        SidebarColumn = rightToLeft ? 1 : 0;
        ContentColumn = rightToLeft ? 0 : 1;
        FirstColumnWidth = rightToLeft ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        SecondColumnWidth = rightToLeft ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
    }
}
