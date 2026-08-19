namespace BookStore.Shared.Constants;

/// <summary>
/// The single authoritative list of permissions the application recognises, paired with the
/// description shown in the roles editor.
/// </summary>
/// <remarks>
/// Seeding used to keep its own copy of this list. Fourteen permissions were enforced in code but
/// missing from that copy, so they were never stored and therefore could never be granted to any
/// role &mdash; which silently disabled completing a sale, the whole barcode module, customer
/// maintenance and stock adjustment. The catalog now lives here, next to the names themselves, and
/// <c>PermissionCatalogTests</c> fails the build if a declared name is ever left out again.
/// </remarks>
public static class PermissionCatalog
{
    /// <summary>
    /// Gets every permission the application recognises, keyed by its stable name.
    /// </summary>
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(PermissionConstants.ProductView, "View products"),
        new(PermissionConstants.ProductCreate, "Create products"),
        new(PermissionConstants.ProductEdit, "Edit products"),
        new(PermissionConstants.ProductDelete, "Delete products"),
        new(PermissionConstants.ProductExport, "Export products"),
        new(PermissionConstants.ProductImport, "Import products"),

        new(PermissionConstants.CategoryView, "View categories"),
        new(PermissionConstants.CategoryCreate, "Create categories"),
        new(PermissionConstants.CategoryEdit, "Edit categories"),
        new(PermissionConstants.CategoryDelete, "Delete categories"),

        new(PermissionConstants.SalesCreate, "Create sales"),
        new(PermissionConstants.SalesComplete, "Complete sales"),
        new(PermissionConstants.SalesSuspend, "Suspend sales"),
        new(PermissionConstants.SalesCancel, "Cancel sales"),
        new(PermissionConstants.SalesApplyDiscount, "Apply sale discounts"),
        new(PermissionConstants.SalesOverridePrice, "Override sale prices"),
        new(PermissionConstants.SalesViewHistory, "View sales history"),

        new(PermissionConstants.InventoryView, "View inventory"),
        new(PermissionConstants.InventoryEdit, "Edit inventory"),
        new(PermissionConstants.InventoryAdjust, "Adjust stock quantities"),
        new(PermissionConstants.InventoryHistory, "View stock ledger"),
        new(PermissionConstants.InventoryExport, "Export inventory"),

        new(PermissionConstants.BarcodeView, "View barcode tools"),
        new(PermissionConstants.BarcodeGenerate, "Generate barcodes"),
        new(PermissionConstants.BarcodePrint, "Print barcodes"),
        new(PermissionConstants.BarcodeSettings, "Manage barcode generation settings"),

        new(PermissionConstants.CustomerView, "View customers"),
        new(PermissionConstants.CustomerCreate, "Create customers"),
        new(PermissionConstants.CustomerEdit, "Edit customers"),
        new(PermissionConstants.CustomerDelete, "Delete customers"),
        new(PermissionConstants.CustomerViewHistory, "View customer purchase history"),

        new(PermissionConstants.SupplierView, "View suppliers"),
        new(PermissionConstants.SupplierCreate, "Create suppliers"),
        new(PermissionConstants.SupplierEdit, "Edit suppliers"),
        new(PermissionConstants.SupplierDelete, "Delete suppliers"),
        new(PermissionConstants.SupplierViewProducts, "View supplier products"),

        new(PermissionConstants.ReportsView, "View reports"),
        new(PermissionConstants.ReportView, "Open reports dashboard"),
        new(PermissionConstants.ReportSales, "View sales reports"),
        new(PermissionConstants.ReportProfit, "View profit reports"),
        new(PermissionConstants.ReportInventory, "View inventory reports"),
        new(PermissionConstants.ReportCustomers, "View customer reports"),
        new(PermissionConstants.ReportCashiers, "View cashier performance reports"),
        new(PermissionConstants.ReportExport, "Export reports"),

        new(PermissionConstants.ReceiptPrint, "Print receipts"),
        new(PermissionConstants.ReceiptReprint, "Reprint receipts"),
        new(PermissionConstants.ReceiptTestPrint, "Run printer test print"),
        new(PermissionConstants.ReceiptSettings, "Manage receipt printer settings"),

        new(PermissionConstants.SettingsView, "View settings"),
        new(PermissionConstants.SettingsStore, "Manage store settings"),
        new(PermissionConstants.SettingsPOS, "Manage POS settings"),
        new(PermissionConstants.SettingsReceipt, "Manage receipt settings"),
        new(PermissionConstants.SettingsPrinter, "Manage printer settings"),
        new(PermissionConstants.SettingsTax, "Manage tax settings"),
        new(PermissionConstants.SettingsCurrency, "Manage currency settings"),
        new(PermissionConstants.SettingsBarcode, "Manage barcode settings"),
        new(PermissionConstants.SettingsInventory, "Manage inventory settings"),
        new(PermissionConstants.SettingsBackup, "Manage backup settings"),
        new(PermissionConstants.SettingsSecurity, "Manage security settings"),
        new(PermissionConstants.SettingsAppearance, "Manage appearance settings"),

        new(PermissionConstants.UsersManage, "Manage users"),
        new(PermissionConstants.RolesManage, "Manage roles"),

        new(PermissionConstants.BackupView, "View database backups"),
        new(PermissionConstants.BackupCreate, "Create database backups"),
        new(PermissionConstants.BackupRestore, "Restore database backups"),
        new(PermissionConstants.BackupDelete, "Delete database backups"),
        new(PermissionConstants.BackupValidate, "Validate database backups"),
        new(PermissionConstants.BackupSettings, "Manage backup retention settings"),

        new(PermissionConstants.AuditView, "View production audit trail"),
        new(PermissionConstants.DataQualityView, "View data quality center")
    ];

    /// <summary>
    /// Gets the permissions granted to the built-in Manager role: everything except user, role and
    /// restore administration.
    /// </summary>
    public static IReadOnlyList<string> ManagerPermissions { get; } =
        All.Select(definition => definition.Name)
            .Where(name => name is not PermissionConstants.UsersManage
                and not PermissionConstants.RolesManage
                and not PermissionConstants.BackupRestore)
            .ToArray();

    /// <summary>
    /// Gets the permissions granted to the built-in Cashier role. This must cover the entire
    /// counter workflow, including completing and suspending a sale.
    /// </summary>
    public static IReadOnlyList<string> CashierPermissions { get; } =
    [
        PermissionConstants.SalesCreate,
        PermissionConstants.SalesComplete,
        PermissionConstants.SalesSuspend,
        PermissionConstants.SalesApplyDiscount,
        PermissionConstants.SalesViewHistory,
        PermissionConstants.ProductView,
        PermissionConstants.CategoryView,
        PermissionConstants.InventoryView,
        PermissionConstants.BarcodeView,
        PermissionConstants.CustomerView,
        PermissionConstants.CustomerCreate,
        PermissionConstants.ReceiptPrint
    ];
}

/// <summary>
/// A permission name paired with its human-readable description.
/// </summary>
/// <param name="Name">The stable permission name stored in the database.</param>
/// <param name="Description">The description shown to administrators.</param>
public sealed record PermissionDefinition(string Name, string Description);
