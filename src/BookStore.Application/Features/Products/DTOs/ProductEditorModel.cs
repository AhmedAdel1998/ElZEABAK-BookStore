namespace BookStore.Application.Features.Products.DTOs;

/// <summary>
/// Represents editable product form data.
/// </summary>
public sealed class ProductEditorModel
{
    /// <summary>Gets or sets the product identifier when editing.</summary>
    public Guid? Id { get; set; }
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
    public string Author { get; set; } = string.Empty;
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
    /// <summary>Gets or sets the tax category placeholder.</summary>
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
    /// <summary>Gets or sets whether the product is active.</summary>
    public bool IsActive { get; set; } = true;
}
