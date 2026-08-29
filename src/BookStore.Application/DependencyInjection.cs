using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using BookStore.Application.Features.Categories.Handlers;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Products.Handlers;
using BookStore.Application.Features.Suppliers.Handlers;
using InventoryHandlers = BookStore.Application.Features.Inventory.Handlers;
using BarcodeHandlers = BookStore.Application.Features.Barcode.Handlers;
using SalesHandlers = BookStore.Application.Features.Sales.Handlers;
using ReportHandlers = BookStore.Application.Features.Reports.Handlers;
using ReceiptHandlers = BookStore.Application.Features.Receipts.Handlers;
using BackupHandlers = BookStore.Application.Features.Backup.Handlers;
using SettingsHandlers = BookStore.Application.Features.Settings.Handlers;
using AuditHandlers = BookStore.Application.Features.Audit.Handlers;
using DataQualityHandlers = BookStore.Application.Features.DataQuality.Handlers;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Features.Sales.Services;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Application.Interfaces;
using BookStore.Application.Features.Administration;

namespace BookStore.Application;

/// <summary>
/// Registers application-layer services used by commands, queries, validation, and mapping.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(_ => { }, typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
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
        services.AddTransient<CreateSupplierHandler>();
        services.AddTransient<UpdateSupplierHandler>();
        services.AddTransient<DeleteSupplierHandler>();
        services.AddTransient<ActivateSupplierHandler>();
        services.AddTransient<DeactivateSupplierHandler>();
        services.AddTransient<GetSuppliersHandler>();
        services.AddTransient<GetSupplierByIdHandler>();
        services.AddTransient<SearchSuppliersHandler>();
        services.AddTransient<GetSupplierProductsHandler>();
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
        services.AddTransient<ImportProductsHandler>();
        services.AddTransient<InventoryHandlers.InventoryMovementService>();
        services.AddTransient<InventoryHandlers.IncreaseStockHandler>();
        services.AddTransient<InventoryHandlers.DecreaseStockHandler>();
        services.AddTransient<InventoryHandlers.AdjustStockHandler>();
        services.AddTransient<InventoryHandlers.GetInventoryHandler>();
        services.AddTransient<InventoryHandlers.GetInventoryHistoryHandler>();
        services.AddTransient<InventoryHandlers.GetLowStockProductsHandler>();
        services.AddTransient<InventoryHandlers.GetOutOfStockProductsHandler>();
        services.AddTransient<InventoryHandlers.GetInventoryDashboardHandler>();
        services.AddTransient<BarcodeHandlers.GenerateBarcodeHandler>();
        services.AddTransient<BarcodeHandlers.ValidateBarcodeHandler>();
        services.AddTransient<BarcodeHandlers.PrintBarcodeHandler>();
        services.AddTransient<BarcodeHandlers.FindProductByBarcodeHandler>();
        services.AddScoped<IPricingService, SalesHandlers.PricingService>();
        services.AddScoped<IPosCartStockService, PosCartStockService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddSingleton<ICheckoutConcurrencyGuard, CheckoutConcurrencyGuard>();
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
        services.AddTransient<SalesHandlers.GetCurrentSaleHandler>();
        services.AddTransient<SalesHandlers.GetHeldSalesHandler>();
        services.AddTransient<SalesHandlers.GetOpenInvoicesHandler>();
        services.AddTransient<SalesHandlers.SwitchInvoiceHandler>();
        services.AddTransient<SalesHandlers.CloseInvoiceHandler>();
        services.AddTransient<SalesHandlers.SaveInvoiceDraftHandler>();
        services.AddTransient<SalesHandlers.GetSaleSummaryHandler>();
        services.AddTransient<SalesHandlers.SelectCustomerForSaleHandler>();
        services.AddTransient<SalesHandlers.ClearCustomerFromSaleHandler>();
        services.AddTransient<ReportHandlers.GetReportsDashboardHandler>();
        services.AddTransient<ReportHandlers.GetSalesSummaryHandler>();
        services.AddTransient<ReportHandlers.GetSalesDetailsHandler>();
        services.AddTransient<ReportHandlers.GetProfitReportHandler>();
        services.AddTransient<ReportHandlers.GetBestSellingProductsHandler>();
        services.AddTransient<ReportHandlers.GetProductSalesHandler>();
        services.AddTransient<ReportHandlers.GetCategorySalesHandler>();
        services.AddTransient<ReportHandlers.GetInventoryReportHandler>();
        services.AddTransient<ReportHandlers.GetInventoryMovementsHandler>();
        services.AddTransient<ReportHandlers.GetLowStockHandler>();
        services.AddTransient<ReportHandlers.GetCustomerReportHandler>();
        services.AddTransient<ReportHandlers.GetCashierPerformanceHandler>();
        services.AddTransient<ReportHandlers.GetPaymentMethodsHandler>();
        services.AddTransient<ReportHandlers.GetDailySalesHandler>();
        services.AddTransient<ReportHandlers.GetHourlySalesHandler>();
        services.AddTransient<ReceiptHandlers.PrintReceiptHandler>();
        services.AddTransient<ReceiptHandlers.ReprintReceiptHandler>();
        services.AddTransient<ReceiptHandlers.TestPrintHandler>();
        services.AddTransient<ReceiptHandlers.RetryPrintHandler>();
        services.AddTransient<ReceiptHandlers.GetReceiptPreviewHandler>();
        services.AddTransient<ReceiptHandlers.GetAvailablePrintersHandler>();
        services.AddTransient<ReceiptHandlers.SearchReceiptSalesHandler>();
        services.AddTransient<BackupHandlers.CreateBackupHandler>();
        services.AddTransient<BackupHandlers.RestoreBackupHandler>();
        services.AddTransient<BackupHandlers.DeleteBackupHandler>();
        services.AddTransient<BackupHandlers.CleanupBackupsHandler>();
        services.AddTransient<BackupHandlers.ValidateBackupHandler>();
        services.AddTransient<BackupHandlers.GetBackupsHandler>();
        services.AddTransient<BackupHandlers.GetBackupDetailsHandler>();
        services.AddTransient<BackupHandlers.RunIntegrityCheckHandler>();
        services.AddTransient<BackupHandlers.GetDatabaseHealthHandler>();
        services.AddTransient<BackupHandlers.RunAutomaticBackupHandler>();
        services.AddTransient<AuditHandlers.RecordAuditEntryHandler>();
        services.AddTransient<AuditHandlers.SearchAuditLogHandler>();
        services.AddTransient<DataQualityHandlers.GetDataQualitySummaryHandler>();
        services.AddSingleton<ISettingsCache, SettingsCache>();
        services.AddSingleton<ISettingsChangedNotifier, SettingsChangedNotifier>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddTransient<SettingsHandlers.SettingsQueryHandler>();
        services.AddTransient<SettingsHandlers.SettingsCommandHandler>();
        services.AddScoped<AdministrationService>();

        return services;
    }
}
