using BookStore.Application.Features.Settings.DTOs;

namespace BookStore.Application.Features.Settings.Commands;

public sealed record UpdateStoreSettingsCommand(StoreSettingsDto Settings);
public sealed record UpdatePOSSettingsCommand(POSSettingsDto Settings);
public sealed record UpdateReceiptSettingsCommand(ReceiptSettingsDto Settings);
public sealed record UpdatePrinterSettingsCommand(PrinterSettingsDto Settings);
public sealed record UpdateTaxSettingsCommand(TaxSettingsDto Settings);
public sealed record UpdateCurrencySettingsCommand(CurrencySettingsDto Settings);
public sealed record UpdateBarcodeSettingsCommand(BarcodeSettingsDto Settings);
public sealed record UpdateInventorySettingsCommand(InventorySettingsDto Settings);
public sealed record UpdateBackupSettingsCommand(BackupSettingsDto Settings);
public sealed record UpdateSecuritySettingsCommand(SecuritySettingsDto Settings);
public sealed record UpdateAppearanceSettingsCommand(AppearanceSettingsDto Settings);
public sealed record ResetSettingsCommand(SettingsCategory? Category = null);
