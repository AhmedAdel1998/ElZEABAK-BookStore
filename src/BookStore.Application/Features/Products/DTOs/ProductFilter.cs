namespace BookStore.Application.Features.Products.DTOs;

/// <summary>
/// Represents product search and filtering criteria.
/// </summary>
public sealed class ProductFilter
{
    /// <summary>Gets or sets the search term.</summary>
    public string? SearchTerm { get; set; }
    /// <summary>Gets or sets active status filter.</summary>
    public bool? IsActive { get; set; }
    /// <summary>Gets or sets whether only low-stock products should be returned.</summary>
    public bool LowStockOnly { get; set; }
    /// <summary>Gets or sets category filter.</summary>
    public Guid? CategoryId { get; set; }
    /// <summary>Gets or sets minimum selling price.</summary>
    public decimal? MinPrice { get; set; }
    /// <summary>Gets or sets maximum selling price.</summary>
    public decimal? MaxPrice { get; set; }
    /// <summary>Gets or sets minimum quantity.</summary>
    public int? MinQuantity { get; set; }
    /// <summary>Gets or sets maximum quantity.</summary>
    public int? MaxQuantity { get; set; }
    /// <summary>Gets or sets page number.</summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>Gets or sets page size.</summary>
    public int PageSize { get; set; } = 25;
}
