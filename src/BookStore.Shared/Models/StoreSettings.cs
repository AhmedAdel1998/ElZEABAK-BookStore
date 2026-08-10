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
    /// Gets or sets the currency code.
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Gets or sets the tax rate.
    /// </summary>
    public decimal TaxRate { get; set; }
}
