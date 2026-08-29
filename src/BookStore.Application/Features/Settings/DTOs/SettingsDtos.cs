namespace BookStore.Application.Features.Settings.DTOs;

/// <summary>Supported settings groups.</summary>
public enum SettingsCategory
{
    Store,
    POS,
    Receipt,
    Printer,
    Tax,
    Currency,
    Barcode,
    Inventory,
    Backup,
    Security,
    Appearance,
    Application
}

public sealed record SettingEntryDto(string Key, SettingsCategory Category, string DataType, string Description, bool IsEncrypted, bool IsSystemSetting, DateTimeOffset? UpdatedAt, string? UpdatedBy);

public sealed class StoreSettingsDto
{
    public string StoreName { get; set; } = "EL ZEABAK BookStore";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
}

public sealed class POSSettingsDto
{
    public string DefaultCustomer { get; set; } = "Walk-in Customer";
    public bool AutoFocusBarcode { get; set; } = true;
    public bool AutoPrintReceipt { get; set; } = true;
    public bool AllowPriceOverride { get; set; } = true;
    public bool AllowDiscount { get; set; } = true;
    public bool RequireCustomerForCreditSales { get; set; }
    public bool StartNewSaleAfterCheckout { get; set; } = true;
}

public sealed class ReceiptSettingsDto
{
    public int PaperWidth { get; set; } = 80;
    public int Copies { get; set; } = 1;
    public bool ShowLogo { get; set; }
    public bool ShowCustomer { get; set; } = true;
    public bool ShowCashier { get; set; } = true;
    public bool ShowBarcode { get; set; } = true;
    public bool PrintQRCode { get; set; }
    public string FooterText { get; set; } = "No returns without receipt.";
    public bool CutPaper { get; set; } = true;
    public bool OpenCashDrawer { get; set; }
}

public sealed class PrinterSettingsDto
{
    public string PrinterName { get; set; } = string.Empty;
    public string PrinterType { get; set; } = "Receipt";
    public string ConnectionType { get; set; } = "Windows";
    public int PrintTimeout { get; set; } = 10000;
}

public sealed class TaxSettingsDto
{
    public bool Enabled { get; set; } = true;
    public decimal DefaultRate { get; set; } = 0.14m;
    public bool TaxIncludedInPrice { get; set; }
    public string TaxDisplayMode { get; set; } = "Separate";
}

public sealed class CurrencySettingsDto
{
    public string CurrencyCode { get; set; } = "EGP";
    public string CurrencySymbol { get; set; } = "EGP";
    public int DecimalPlaces { get; set; } = 2;
    public string CurrencyPosition { get; set; } = "Before";
}

public sealed class BarcodeSettingsDto
{
    public string DefaultFormat { get; set; } = "Code128";
    public string Prefix { get; set; } = "BK";
    public int StartingNumber { get; set; } = 100000;
    public int Length { get; set; } = 12;
    public double LabelWidthMm { get; set; } = 50;
    public double LabelHeightMm { get; set; } = 25;
    public string PrinterName { get; set; } = string.Empty;
    public int ScanTimeout { get; set; } = 80;
    public bool AutoGenerate { get; set; } = true;
    public bool ManualGenerationEnabled { get; set; } = true;
}

public sealed class InventorySettingsDto
{
    public int LowStockThreshold { get; set; } = 5;
    public bool AllowNegativeStock { get; set; }
    public bool AutoInventoryAdjustment { get; set; }
    public int InventoryWarningThreshold { get; set; } = 5;
}

public sealed class BackupSettingsDto
{
    public bool Enabled { get; set; } = true;
    public string Frequency { get; set; } = "Daily";
    public string BackupLocation { get; set; } = "%ProgramData%\\BookStore\\Backups";
    public int RetentionCount { get; set; } = 10;
    public bool ValidateAfterBackup { get; set; } = true;
    public bool CreatePreRestoreBackup { get; set; } = true;
}

public sealed class SecuritySettingsDto
{
    public int SessionTimeout { get; set; } = 30;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutDuration { get; set; } = 15;
    public string PasswordPolicy { get; set; } = "Minimum8";
}

public sealed class AppearanceSettingsDto
{
    public string Theme { get; set; } = "Light";
    public string Language { get; set; } = "ar-EG";
}

public sealed class ApplicationSettingsDto
{
    public string ApplicationName { get; set; } = "BookStore POS";
    public string ApplicationVersion { get; set; } = "1.0.0";
    public bool AutoUpdateEnabled { get; set; }
    public string DatabaseVersion { get; set; } = string.Empty;
    public string InstallationPath { get; set; } = AppContext.BaseDirectory;
    public string DatabasePath { get; set; } = string.Empty;
}

public sealed class SettingsSnapshotDto
{
    public StoreSettingsDto Store { get; set; } = new();
    public POSSettingsDto POS { get; set; } = new();
    public ReceiptSettingsDto Receipt { get; set; } = new();
    public PrinterSettingsDto Printer { get; set; } = new();
    public TaxSettingsDto Tax { get; set; } = new();
    public CurrencySettingsDto Currency { get; set; } = new();
    public BarcodeSettingsDto Barcode { get; set; } = new();
    public InventorySettingsDto Inventory { get; set; } = new();
    public BackupSettingsDto Backup { get; set; } = new();
    public SecuritySettingsDto Security { get; set; } = new();
    public AppearanceSettingsDto Appearance { get; set; } = new();
    public ApplicationSettingsDto Application { get; set; } = new();
}
