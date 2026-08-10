namespace BookStore.Application.Features.Products.DTOs;

/// <summary>
/// Represents product data for details and editing screens.
/// </summary>
public class ProductDto
{
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the barcode.</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Gets or sets the ISBN.</summary>
    public string? ISBN { get; set; }
    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the subtitle.</summary>
    public string? Subtitle { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the author.</summary>
    public string? Author { get; set; }
    /// <summary>Gets or sets the publisher.</summary>
    public string? Publisher { get; set; }
    /// <summary>Gets or sets the language.</summary>
    public string? Language { get; set; }
    /// <summary>Gets or sets the edition.</summary>
    public string? Edition { get; set; }
    /// <summary>Gets or sets the publish date.</summary>
    public DateOnly? PublishDate { get; set; }
    /// <summary>Gets or sets the purchase price.</summary>
    public decimal PurchasePrice { get; set; }
    /// <summary>Gets or sets the selling price.</summary>
    public decimal SellingPrice { get; set; }
    /// <summary>Gets or sets the tax category.</summary>
    public string? TaxCategory { get; set; }
    /// <summary>Gets or sets the quantity.</summary>
    public int Quantity { get; set; }
    /// <summary>Gets or sets the minimum stock.</summary>
    public int MinimumStock { get; set; }
    /// <summary>Gets or sets the shelf location.</summary>
    public string? ShelfLocation { get; set; }
    /// <summary>Gets or sets the image path.</summary>
    public string? ImagePath { get; set; }
    /// <summary>Gets or sets the category identifier.</summary>
    public Guid CategoryId { get; set; }
    /// <summary>Gets or sets the category name.</summary>
    public string? CategoryName { get; set; }
    /// <summary>Gets or sets a value indicating whether the product is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the created date.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets the updated date.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>Gets the status text.</summary>
    public string Status => IsActive ? "Active" : "Inactive";
    /// <summary>Gets a value indicating whether the product is low stock.</summary>
    public bool IsLowStock => Quantity <= MinimumStock;
}
