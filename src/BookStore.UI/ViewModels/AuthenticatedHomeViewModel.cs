using System.Collections.ObjectModel;
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
    private readonly DispatcherTimer _clockTimer;

    [ObservableProperty]
    private bool isSidebarCollapsed;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string currentTime = string.Empty;

    [ObservableProperty]
    private string databaseStatus = "Connected";

    [ObservableProperty]
    private string internetStatus = "Offline";

    [ObservableProperty]
    private int pendingBackgroundTasks;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string loadingText = "Loading...";

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
        IThemeService themeService)
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
        _loadingService.StateChanged += OnLoadingStateChanged;
        Title = ApplicationConstants.ApplicationName;
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
        _notificationService.Show("Theme", $"{_themeService.CurrentTheme} theme applied.", NotificationSeverity.Information);
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
    private Task NavigateDashboardAsync() => NavigateAsync<DashboardViewModel>("Dashboard");

    /// <summary>
    /// Navigates to products.
    /// </summary>
    [RelayCommand]
    private Task NavigateProductsAsync() => NavigateAsync<ProductListViewModel>("Products > Product List");

    /// <summary>
    /// Navigates to inventory.
    /// </summary>
    [RelayCommand]
    private Task NavigateInventoryAsync() => NavigateAsync<InventoryDashboardViewModel>("Inventory");

    /// <summary>
    /// Navigates to barcode management.
    /// </summary>
    [RelayCommand]
    private Task NavigateBarcodeAsync() => NavigateAsync<BarcodePreviewViewModel>("Barcode");

    /// <summary>
    /// Navigates to sales.
    /// </summary>
    [RelayCommand]
    private Task NavigateSalesAsync() => NavigateAsync<POSViewModel>("Sales > POS");

    /// <summary>
    /// Navigates to reports.
    /// </summary>
    [RelayCommand]
    private Task NavigateReportsAsync() => NavigateAsync<ReportsViewModel>("Reports");

    /// <summary>
    /// Navigates to backup.
    /// </summary>
    [RelayCommand]
    private Task NavigateBackupAsync() => NavigateAsync<BackupViewModel>("Backup");

    /// <summary>
    /// Navigates to change password.
    /// </summary>
    [RelayCommand]
    private Task ChangePasswordAsync() => NavigateAsync<ChangePasswordViewModel>("Settings > Change Password");

    private async Task NavigateAsync<TViewModel>(string breadcrumb)
        where TViewModel : BaseViewModel
    {
        _loadingService.Show("Opening page...");
        try
        {
            await _shellNavigationService.NavigateToAsync<TViewModel>(breadcrumb);
            foreach (var item in MenuItems)
            {
                item.IsActive = string.Equals(item.Breadcrumb, breadcrumb, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            _loadingService.Hide();
        }
    }

    private void BuildNavigationItems()
    {
        AddMenuItem("Dashboard", "D", null, NavigateDashboardCommand, "Dashboard");
        AddMenuItem("Categories", "C", PermissionConstants.CategoryView, new AsyncRelayCommand(() => NavigateAsync<CategoryListViewModel>("Categories")), "Categories");
        AddMenuItem("Products", "P", PermissionConstants.ProductView, NavigateProductsCommand, "Products > Product List");
        AddMenuItem("Inventory", "I", PermissionConstants.InventoryView, NavigateInventoryCommand, "Inventory");
        AddMenuItem("Barcode", "BC", PermissionConstants.BarcodeView, NavigateBarcodeCommand, "Barcode");
        AddMenuItem("Sales (POS)", "S", PermissionConstants.SalesCreate, NavigateSalesCommand, "Sales > POS");
        AddMenuItem("Customers", "CU", PermissionConstants.CustomerView, new AsyncRelayCommand(() => NavigateAsync<CustomerListViewModel>("Customers")), "Customers");
        AddMenuItem("Suppliers", "SU", PermissionConstants.SupplierView, new AsyncRelayCommand(() => NavigateAsync<SuppliersViewModel>("Suppliers")), "Suppliers");
        AddMenuItem("Reports", "R", PermissionConstants.ReportsView, NavigateReportsCommand, "Reports");
        AddMenuItem("Settings", "ST", PermissionConstants.SettingsView, new AsyncRelayCommand(() => NavigateAsync<SettingsViewModel>("Settings")), "Settings");
        AddMenuItem("Users", "U", PermissionConstants.UsersManage, new AsyncRelayCommand(() => NavigateAsync<UsersViewModel>("Settings > Users")), "Settings > Users");
        AddMenuItem("Roles", "RO", PermissionConstants.RolesManage, new AsyncRelayCommand(() => NavigateAsync<RolesViewModel>("Settings > Roles")), "Settings > Roles");
        AddMenuItem("Backup", "B", PermissionConstants.BackupDatabase, NavigateBackupCommand, "Backup");
        AddMenuItem("Logout", "L", null, LogoutCommand, "Logout");
    }

    private void AddMenuItem(string text, string icon, string? permission, System.Windows.Input.ICommand command, string breadcrumb)
    {
        if (permission is not null && !_authorizationService.HasPermission(permission))
        {
            return;
        }

        MenuItems.Add(new NavigationItem
        {
            Text = text,
            Icon = icon,
            RequiredPermission = permission,
            Command = command,
            Breadcrumb = breadcrumb
        });
    }

    private void OnLoadingStateChanged(object? sender, EventArgs e)
    {
        IsLoading = _loadingService.IsLoading;
        LoadingText = _loadingService.LoadingText;
    }
}
