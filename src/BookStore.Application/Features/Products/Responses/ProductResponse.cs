namespace BookStore.Application.Features.Products.Responses;

/// <summary>
/// Represents a product command response.
/// </summary>
public sealed class ProductResponse
{
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the barcode.</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Gets or sets the product title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the ISBN.</summary>
    public string? ISBN { get; set; }
    /// <summary>Gets or sets whether the product is active.</summary>
    public bool IsActive { get; set; }
}
