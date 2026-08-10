using BookStore.Application.Interfaces;
using BookStore.Application.Features.Categories.Handlers;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Products.Handlers;
using BarcodeHandlers = BookStore.Application.Features.Barcode.Handlers;
using SalesHandlers = BookStore.Application.Features.Sales.Handlers;
using BookStore.UI.Dialogs;
using BookStore.UI.Icons;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using BookStore.UI.Validation;
using BookStore.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.UI;

/// <summary>
/// Registers presentation-layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds WPF presentation services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IShellNavigationService, ShellNavigationService>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IApplicationShutdownService, ApplicationShutdownService>();
        services.AddSingleton<ISessionTimeoutService, SessionTimeoutService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ILoadingService, LoadingService>();
        services.AddSingleton<IWindowStateService, WindowStateService>();
        services.AddSingleton<IWindowService>(provider => (IWindowService)provider.GetRequiredService<IWindowStateService>());
        services.AddSingleton<IIconRegistry, IconRegistry>();
        services.AddSingleton<IValidationMessageAdapter, ValidationMessageAdapter>();
        services.AddSingleton<ICategoryNavigationState, CategoryNavigationState>();
        services.AddSingleton<IProductNavigationState, ProductNavigationState>();
        services.AddSingleton<ICustomerNavigationState, CustomerNavigationState>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<IDialogService>(provider => provider.GetRequiredService<DialogService>());
        services.AddSingleton<IMessageDialogService>(provider => provider.GetRequiredService<DialogService>());
        services.AddSingleton<IConfirmationDialogService>(provider => provider.GetRequiredService<DialogService>());
        services.AddSingleton<IErrorDialogService>(provider => provider.GetRequiredService<DialogService>());

        services.AddTransient<CreateCategoryHandler>();
        services.AddTransient<UpdateCategoryHandler>();
        services.AddTransient<DeleteCategoryHandler>();
        services.AddTransient<ActivateCategoryHandler>();
        services.AddTransient<DeactivateCategoryHandler>();
        services.AddTransient<GetCategoriesHandler>();
        services.AddTransient<GetCategoryByIdHandler>();
        services.AddTransient<SearchCategoriesHandler>();
        services.AddTransient<CreateCustomerHandler>();
        services.AddTransient<UpdateCustomerHandler>();
        services.AddTransient<DeleteCustomerHandler>();
        services.AddTransient<ActivateCustomerHandler>();
        services.AddTransient<DeactivateCustomerHandler>();
        services.AddTransient<GetCustomersHandler>();
        services.AddTransient<GetCustomerByIdHandler>();
        services.AddTransient<SearchCustomersHandler>();
        services.AddTransient<GetCustomerSalesHistoryHandler>();
        services.AddTransient<CreateProductHandler>();
        services.AddTransient<UpdateProductHandler>();
        services.AddTransient<DeleteProductHandler>();
        services.AddTransient<ActivateProductHandler>();
        services.AddTransient<DeactivateProductHandler>();
        services.AddTransient<DuplicateProductHandler>();
        services.AddTransient<GetProductsHandler>();
        services.AddTransient<GetProductByIdHandler>();
        services.AddTransient<SearchProductsHandler>();
        services.AddTransient<GetLowStockProductsHandler>();
        services.AddTransient<BarcodeHandlers.GenerateBarcodeHandler>();
        services.AddTransient<BarcodeHandlers.ValidateBarcodeHandler>();
        services.AddTransient<BarcodeHandlers.PrintBarcodeHandler>();
        services.AddTransient<BarcodeHandlers.FindProductByBarcodeHandler>();
        services.AddTransient<BarcodeHandlers.GetBarcodeSettingsHandler>();
        services.AddTransient<SalesHandlers.StartSaleHandler>();
        services.AddTransient<SalesHandlers.AddItemHandler>();
        services.AddTransient<SalesHandlers.UpdateItemQuantityHandler>();
        services.AddTransient<SalesHandlers.RemoveItemHandler>();
        services.AddTransient<SalesHandlers.ApplyLineDiscountHandler>();
        services.AddTransient<SalesHandlers.ApplyInvoiceDiscountHandler>();
        services.AddTransient<SalesHandlers.CancelSaleHandler>();
        services.AddTransient<SalesHandlers.SuspendSaleHandler>();
        services.AddTransient<SalesHandlers.ResumeSaleHandler>();
        services.AddTransient<SalesHandlers.CompleteSaleHandler>();
        services.AddTransient<SalesHandlers.SearchProductHandler>();
        services.AddTransient<SalesHandlers.GetHeldSalesHandler>();
        services.AddTransient<SalesHandlers.SelectCustomerForSaleHandler>();
        services.AddTransient<SalesHandlers.ClearCustomerFromSaleHandler>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<AuthenticatedHomeViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<CategoryListViewModel>();
        services.AddTransient<CategoryEditorViewModel>();
        services.AddTransient<CategoryDetailsViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<InventoryDashboardViewModel>();
        services.AddTransient<InventoryListViewModel>();
        services.AddTransient<InventoryAdjustmentViewModel>();
        services.AddTransient<InventoryHistoryViewModel>();
        services.AddTransient<BarcodePreviewViewModel>();
        services.AddTransient<BarcodeSettingsViewModel>();
        services.AddTransient<BarcodeLabelPrintViewModel>();
        services.AddTransient<POSViewModel>();
        services.AddTransient<CustomerListViewModel>();
        services.AddTransient<CustomerEditorViewModel>();
        services.AddTransient<CustomerDetailsViewModel>();
        services.AddTransient<CustomerSelectionViewModel>();
        services.AddTransient<SalesViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<RolesViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductEditorViewModel>();
        services.AddTransient<ProductDetailsViewModel>();
        services.AddSingleton<IReadOnlyDictionary<Type, Func<BaseViewModel>>>(provider =>
            new Dictionary<Type, Func<BaseViewModel>>
            {
                [typeof(LoginViewModel)] = () => provider.GetRequiredService<LoginViewModel>(),
                [typeof(AuthenticatedHomeViewModel)] = () => provider.GetRequiredService<AuthenticatedHomeViewModel>(),
                [typeof(ChangePasswordViewModel)] = () => provider.GetRequiredService<ChangePasswordViewModel>(),
                [typeof(CategoryListViewModel)] = () => provider.GetRequiredService<CategoryListViewModel>(),
                [typeof(CategoryEditorViewModel)] = () => provider.GetRequiredService<CategoryEditorViewModel>(),
                [typeof(CategoryDetailsViewModel)] = () => provider.GetRequiredService<CategoryDetailsViewModel>(),
                [typeof(DashboardViewModel)] = () => provider.GetRequiredService<DashboardViewModel>(),
                [typeof(CategoriesViewModel)] = () => provider.GetRequiredService<CategoriesViewModel>(),
                [typeof(ProductsViewModel)] = () => provider.GetRequiredService<ProductsViewModel>(),
                [typeof(InventoryViewModel)] = () => provider.GetRequiredService<InventoryViewModel>(),
                [typeof(InventoryDashboardViewModel)] = () => provider.GetRequiredService<InventoryDashboardViewModel>(),
                [typeof(InventoryListViewModel)] = () => provider.GetRequiredService<InventoryListViewModel>(),
                [typeof(InventoryAdjustmentViewModel)] = () => provider.GetRequiredService<InventoryAdjustmentViewModel>(),
                [typeof(InventoryHistoryViewModel)] = () => provider.GetRequiredService<InventoryHistoryViewModel>(),
                [typeof(BarcodePreviewViewModel)] = () => provider.GetRequiredService<BarcodePreviewViewModel>(),
                [typeof(BarcodeSettingsViewModel)] = () => provider.GetRequiredService<BarcodeSettingsViewModel>(),
                [typeof(BarcodeLabelPrintViewModel)] = () => provider.GetRequiredService<BarcodeLabelPrintViewModel>(),
                [typeof(POSViewModel)] = () => provider.GetRequiredService<POSViewModel>(),
                [typeof(CustomerListViewModel)] = () => provider.GetRequiredService<CustomerListViewModel>(),
                [typeof(CustomerEditorViewModel)] = () => provider.GetRequiredService<CustomerEditorViewModel>(),
                [typeof(CustomerDetailsViewModel)] = () => provider.GetRequiredService<CustomerDetailsViewModel>(),
                [typeof(CustomerSelectionViewModel)] = () => provider.GetRequiredService<CustomerSelectionViewModel>(),
                [typeof(SalesViewModel)] = () => provider.GetRequiredService<SalesViewModel>(),
                [typeof(CustomersViewModel)] = () => provider.GetRequiredService<CustomersViewModel>(),
                [typeof(SuppliersViewModel)] = () => provider.GetRequiredService<SuppliersViewModel>(),
                [typeof(ReportsViewModel)] = () => provider.GetRequiredService<ReportsViewModel>(),
                [typeof(SettingsViewModel)] = () => provider.GetRequiredService<SettingsViewModel>(),
                [typeof(UsersViewModel)] = () => provider.GetRequiredService<UsersViewModel>(),
                [typeof(RolesViewModel)] = () => provider.GetRequiredService<RolesViewModel>(),
                [typeof(BackupViewModel)] = () => provider.GetRequiredService<BackupViewModel>(),
                [typeof(ProductListViewModel)] = () => provider.GetRequiredService<ProductListViewModel>(),
                [typeof(ProductEditorViewModel)] = () => provider.GetRequiredService<ProductEditorViewModel>(),
                [typeof(ProductDetailsViewModel)] = () => provider.GetRequiredService<ProductDetailsViewModel>()
            });

        return services;
    }
}
