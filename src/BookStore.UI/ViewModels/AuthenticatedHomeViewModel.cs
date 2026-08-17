using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly DispatcherTimer _clockTimer;

    [ObservableProperty]
    private bool isSidebarCollapsed;

    [ObservableProperty]
    private string searchText = string.Empty;

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
        ILocalizationService localizationService)
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
    /// Toggles sidebar collapsed state.
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

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
            var breadcrumb = _localizationService.T(breadcrumbKey);
            await _shellNavigationService.NavigateToAsync<TViewModel>(breadcrumb);
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
        AddMenuItem("Nav.Dashboard", "D", null, NavigateDashboardCommand);
        AddMenuItem("Nav.Categories", "C", PermissionConstants.CategoryView, new AsyncRelayCommand(() => NavigateAsync<CategoryListViewModel>("Nav.Categories")));
        AddMenuItem("Nav.Products", "P", PermissionConstants.ProductView, NavigateProductsCommand);
        AddMenuItem("Nav.Inventory", "I", PermissionConstants.InventoryView, NavigateInventoryCommand);
        AddMenuItem("Nav.Barcode", "BC", PermissionConstants.BarcodeView, NavigateBarcodeCommand);
        AddMenuItem("Nav.SalesPOS", "S", PermissionConstants.SalesCreate, NavigateSalesCommand);
        AddMenuItem("Nav.Customers", "CU", PermissionConstants.CustomerView, new AsyncRelayCommand(() => NavigateAsync<CustomerListViewModel>("Nav.Customers")));
        AddMenuItem("Nav.Suppliers", "SU", PermissionConstants.SupplierView, new AsyncRelayCommand(() => NavigateAsync<SupplierListViewModel>("Nav.Suppliers")));
        AddMenuItem("Nav.Reports", "R", PermissionConstants.ReportView, NavigateReportsCommand);
        AddMenuItem("Nav.Receipts", "RC", PermissionConstants.ReceiptReprint, NavigateReceiptsCommand);
        AddMenuItem("Nav.Settings", "ST", PermissionConstants.SettingsView, new AsyncRelayCommand(() => NavigateAsync<SettingsViewModel>("Nav.Settings")));
        AddMenuItem("Nav.Users", "U", PermissionConstants.UsersManage, new AsyncRelayCommand(() => NavigateAsync<UsersViewModel>("Nav.Users")));
        AddMenuItem("Nav.Roles", "RO", PermissionConstants.RolesManage, new AsyncRelayCommand(() => NavigateAsync<RolesViewModel>("Nav.Roles")));
        AddMenuItem("Nav.Backup", "B", PermissionConstants.BackupView, NavigateBackupCommand);
        AddMenuItem("Nav.Audit", "A", PermissionConstants.AuditView, NavigateAuditCommand);
        AddMenuItem("Nav.DataQuality", "DQ", PermissionConstants.DataQualityView, NavigateDataQualityCommand);
        AddMenuItem("Nav.Logout", "L", null, LogoutCommand);
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

    private void UpdateShellColumns()
    {
        SidebarColumn = _localizationService.IsRightToLeft ? 1 : 0;
        ContentColumn = _localizationService.IsRightToLeft ? 0 : 1;
    }
}
