using System.Globalization;
using System.Text.RegularExpressions;
using BookStore.Application.Features.Settings.DTOs;
using FluentValidation;

namespace BookStore.Application.Features.Settings.Validators;

public sealed partial class StoreSettingsValidator : AbstractValidator<StoreSettingsDto>
{
    public StoreSettingsValidator()
    {
        RuleFor(settings => settings.StoreName).NotEmpty().MaximumLength(200);
        RuleFor(settings => settings.Phone).Must(value => string.IsNullOrWhiteSpace(value) || PhoneRegex().IsMatch(value)).WithMessage("Phone format is invalid.");
        RuleFor(settings => settings.Email).EmailAddress().When(settings => !string.IsNullOrWhiteSpace(settings.Email));
        RuleFor(settings => settings.LogoPath).MaximumLength(500);
    }

    [GeneratedRegex(@"^[0-9\+\-\s\(\)]{6,30}$")]
    private static partial Regex PhoneRegex();
}

public sealed class POSSettingsValidator : AbstractValidator<POSSettingsDto>
{
    public POSSettingsValidator()
    {
        RuleFor(settings => settings.DefaultCustomer).NotEmpty().MaximumLength(200);
    }
}

public sealed class ReceiptSettingsValidator : AbstractValidator<ReceiptSettingsDto>
{
    public ReceiptSettingsValidator()
    {
        RuleFor(settings => settings.PaperWidth).Must(width => width is 58 or 80).WithMessage("Receipt paper width must be 58mm or 80mm.");
        RuleFor(settings => settings.Copies).InclusiveBetween(1, 5);
        RuleFor(settings => settings.FooterText).MaximumLength(500);
    }
}

public sealed class PrinterSettingsValidator : AbstractValidator<PrinterSettingsDto>
{
    public PrinterSettingsValidator()
    {
        RuleFor(settings => settings.PrinterName).MaximumLength(260);
        RuleFor(settings => settings.PrinterType).NotEmpty().MaximumLength(50);
        RuleFor(settings => settings.ConnectionType).NotEmpty().MaximumLength(50);
        RuleFor(settings => settings.PrintTimeout).InclusiveBetween(1000, 120000);
    }
}

public sealed class TaxSettingsValidator : AbstractValidator<TaxSettingsDto>
{
    public TaxSettingsValidator()
    {
        RuleFor(settings => settings.DefaultRate).InclusiveBetween(0m, 1m);
        RuleFor(settings => settings.TaxDisplayMode).NotEmpty().Must(mode => mode is "Separate" or "Included" or "Hidden");
    }
}

public sealed class CurrencySettingsValidator : AbstractValidator<CurrencySettingsDto>
{
    public CurrencySettingsValidator()
    {
        RuleFor(settings => settings.CurrencyCode).Must(BeCurrencyCode).WithMessage("Currency code must be a valid ISO currency code.");
        RuleFor(settings => settings.CurrencySymbol).NotEmpty().MaximumLength(10);
        RuleFor(settings => settings.DecimalPlaces).InclusiveBetween(0, 4);
        RuleFor(settings => settings.CurrencyPosition).Must(position => position is "Before" or "After");
    }

    private static bool BeCurrencyCode(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length == 3
            && CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(culture => new RegionInfo(culture.Name).ISOCurrencySymbol)
                .Any(code => string.Equals(code, value, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed partial class BarcodeSettingsValidator : AbstractValidator<BarcodeSettingsDto>
{
    public BarcodeSettingsValidator()
    {
        RuleFor(settings => settings.DefaultFormat).NotEmpty().Must(format => format is "Code128" or "Code39" or "Ean13" or "Ean8");
        RuleFor(settings => settings.Prefix).Must(value => string.IsNullOrWhiteSpace(value) || PrefixRegex().IsMatch(value)).WithMessage("Barcode prefix is invalid.");
        RuleFor(settings => settings.StartingNumber).GreaterThan(0);
        RuleFor(settings => settings.ScanTimeout).InclusiveBetween(20, 5000);
    }

    [GeneratedRegex(@"^[A-Z0-9\-]{0,12}$", RegexOptions.IgnoreCase)]
    private static partial Regex PrefixRegex();
}

public sealed class InventorySettingsValidator : AbstractValidator<InventorySettingsDto>
{
    public InventorySettingsValidator()
    {
        RuleFor(settings => settings.LowStockThreshold).GreaterThanOrEqualTo(0);
        RuleFor(settings => settings.InventoryWarningThreshold).GreaterThanOrEqualTo(0);
    }
}

public sealed class BackupSettingsValidator : AbstractValidator<BackupSettingsDto>
{
    public BackupSettingsValidator()
    {
        RuleFor(settings => settings.RetentionCount).GreaterThan(0);
        RuleFor(settings => settings.BackupLocation).NotEmpty().MaximumLength(500);
        RuleFor(settings => settings.Frequency).Must(frequency => frequency is "Disabled" or "Daily" or "Weekly");
    }
}

public sealed class SecuritySettingsValidator : AbstractValidator<SecuritySettingsDto>
{
    public SecuritySettingsValidator()
    {
        RuleFor(settings => settings.SessionTimeout).GreaterThan(0);
        RuleFor(settings => settings.MaxLoginAttempts).InclusiveBetween(1, 20);
        RuleFor(settings => settings.LockoutDuration).GreaterThan(0);
        RuleFor(settings => settings.PasswordPolicy).NotEmpty().MaximumLength(100);
    }
}

public sealed class AppearanceSettingsValidator : AbstractValidator<AppearanceSettingsDto>
{
    public AppearanceSettingsValidator()
    {
        RuleFor(settings => settings.Theme).Must(theme => theme is "Light" or "Dark" or "System");
        RuleFor(settings => settings.Language).NotEmpty().MaximumLength(20);
    }
}
