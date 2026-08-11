using BookStore.Application.Interfaces;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Infrastructure.Authentication;
using BookStore.Infrastructure.Backup;
using BookStore.Infrastructure.Barcode;
using BookStore.Infrastructure.Configuration;
using BookStore.Infrastructure.Logging;
using BookStore.Infrastructure.Printing;
using BookStore.Infrastructure.Printing.ESCPos;
using BookStore.Infrastructure.Printing.Services;
using BookStore.Infrastructure.Printing.Thermal;
using BookStore.Infrastructure.Sales;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Infrastructure;

/// <summary>
/// Registers infrastructure-layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ApplicationLogger>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IRememberMeStore, RememberMeStore>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuthorizationService, PermissionService>();
        services.AddSingleton<IApplicationFolderService, ApplicationFolderService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<RoleService>();
        services.AddScoped<IBarcodeService, BarcodeGeneratorService>();
        services.AddScoped<IBarcodeScannerService, BarcodeScannerService>();
        services.AddScoped<IBarcodeLabelPrintService, BarcodeLabelPrintService>();
#pragma warning disable CA1416
        services.AddScoped<IReceiptPrinter, WindowsReceiptPrinter>();
        services.AddScoped<IPrinterDiscoveryService, PrinterDiscoveryService>();
#pragma warning restore CA1416
        services.AddScoped<IReceiptFormatter, ReceiptFormatter>();
        services.AddScoped<IPrintQueueService, PrintQueueService>();
        services.AddScoped<ICashDrawerService, CashDrawerService>();
        services.AddScoped<IReceiptCodeService, ReceiptCodeService>();
        services.AddScoped<EscPosReceiptCommandBuilder>();
        services.AddSingleton<IPosSaleSessionStore, PosSaleSessionStore>();
        services.AddSingleton<IReceiptPreparationService, ReceiptPreparationService>();
        services.AddScoped<ReceiptPrinter>();
        services.AddScoped<BarcodePrinter>();
        services.AddScoped<DatabaseBackupService>();
        services.AddScoped<DatabaseRestoreService>();

        return services;
    }
}
