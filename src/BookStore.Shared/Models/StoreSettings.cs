namespace BookStore.Shared.Models;

/// <summary>
/// Represents store-level settings.
/// </summary>
public class StoreSettings
{
    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string Name { get; set; } = "BookStore POS";

    /// <summary>
    /// Gets or sets the store address printed on receipts.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store phone printed on receipts.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax registration number printed on receipts.
    /// </summary>
    public string TaxNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional logo path reserved for printer support.
    /// </summary>
    public string LogoPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Gets or sets the tax rate.
    /// </summary>
    public decimal TaxRate { get; set; }
}
