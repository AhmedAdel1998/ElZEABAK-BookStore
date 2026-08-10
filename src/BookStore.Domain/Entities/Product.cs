using BookStore.Domain.Common;
using BookStore.Domain.Events;
using BookStore.Domain.Exceptions;
using BookStore.Domain.ValueObjects;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents a sellable bookstore product.
/// </summary>
public class Product : BaseEntity, IAggregateRoot
{
    private Product()
    {
        Barcode = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Product"/> class.
    /// </summary>
    /// <param name="barcode">The product barcode.</param>
    /// <param name="title">The product title.</param>
    /// <param name="purchasePrice">The purchase price.</param>
    /// <param name="sellingPrice">The selling price.</param>
    /// <param name="categoryId">The category identifier.</param>
    public Product(Barcode barcode, string title, decimal purchasePrice, decimal sellingPrice, Guid categoryId)
    {
        Barcode = barcode ?? throw new ValidationException("Barcode is required.");
        SetTitle(title);
        ValidateMoney(purchasePrice, nameof(PurchasePrice));
        ValidateMoney(sellingPrice, nameof(SellingPrice));
        PurchasePrice = purchasePrice;
        SellingPrice = sellingPrice;
        CategoryId = categoryId;
        Quantity = 0;
        MinimumStock = 0;
        IsActive = true;
        AddDomainEvent(new ProductCreated(Id));
    }

    /// <summary>
    /// Gets the product barcode.
    /// </summary>
    public Barcode Barcode { get; private set; }

    /// <summary>
    /// Gets the product ISBN.
    /// </summary>
    public ISBN? ISBN { get; private set; }

    /// <summary>
    /// Gets the product title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional product subtitle.
    /// </summary>
    public string? Subtitle { get; private set; }

    /// <summary>
    /// Gets the product description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the purchase price.
    /// </summary>
    public decimal PurchasePrice { get; private set; }

    /// <summary>
    /// Gets the selling price.
    /// </summary>
    public decimal SellingPrice { get; private set; }

    /// <summary>
    /// Gets the current stock quantity.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Gets the minimum stock threshold.
    /// </summary>
    public int MinimumStock { get; private set; }

    /// <summary>
    /// Gets the product image path.
    /// </summary>
    public string? ImagePath { get; private set; }

    /// <summary>
    /// Gets the shelf location.
    /// </summary>
    public string? ShelfLocation { get; private set; }

    /// <summary>
    /// Gets the publisher.
    /// </summary>
    public string? Publisher { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public string? Author { get; private set; }

    /// <summary>
    /// Gets the language.
    /// </summary>
    public string? Language { get; private set; }

    /// <summary>
    /// Gets the product edition.
    /// </summary>
    public string? Edition { get; private set; }

    /// <summary>
    /// Gets the tax category reserved for future tax configuration.
    /// </summary>
    public string? TaxCategory { get; private set; }

    /// <summary>
    /// Gets the publish date.
    /// </summary>
    public DateOnly? PublishDate { get; private set; }

    /// <summary>
    /// Gets the category identifier.
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Gets the product category.
    /// </summary>
    public Category? Category { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the product is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Increases stock by the specified quantity.
    /// </summary>
    /// <param name="quantity">The quantity to add.</param>
    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("Stock increase quantity must be greater than zero.");
        }

        Quantity += quantity;
        MarkUpdated();
        AddDomainEvent(new ProductStockChanged(Id, Quantity));
    }

    /// <summary>
    /// Decreases stock by the specified quantity.
    /// </summary>
    /// <param name="quantity">The quantity to remove.</param>
    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("Stock decrease quantity must be greater than zero.");
        }

        if (Quantity - quantity < 0)
        {
            throw new BusinessRuleException("Product quantity cannot be negative.");
        }

        Quantity -= quantity;
        MarkUpdated();
        AddDomainEvent(new ProductStockChanged(Id, Quantity));
    }

    /// <summary>
    /// Updates product prices.
    /// </summary>
    /// <param name="purchasePrice">The purchase price.</param>
    /// <param name="sellingPrice">The selling price.</param>
    public void UpdatePrice(decimal purchasePrice, decimal sellingPrice)
    {
        ValidateMoney(purchasePrice, nameof(PurchasePrice));
        ValidateMoney(sellingPrice, nameof(SellingPrice));
        PurchasePrice = purchasePrice;
        SellingPrice = sellingPrice;
        MarkUpdated();
    }

    /// <summary>
    /// Updates the product barcode.
    /// </summary>
    /// <param name="barcode">The barcode.</param>
    public void UpdateBarcode(Barcode barcode)
    {
        Barcode = barcode ?? throw new ValidationException("Barcode is required.");
        MarkUpdated();
    }

    /// <summary>
    /// Updates the product title and subtitle.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    public void UpdateTitle(string title, string? subtitle)
    {
        SetTitle(title);
        Subtitle = subtitle;
        MarkUpdated();
    }

    /// <summary>
    /// Activates the product.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    /// <summary>
    /// Deactivates the product.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    /// <summary>
    /// Updates descriptive product details.
    /// </summary>
    /// <param name="isbn">The ISBN.</param>
    /// <param name="description">The description.</param>
    /// <param name="author">The author.</param>
    /// <param name="publisher">The publisher.</param>
    /// <param name="language">The language.</param>
    /// <param name="publishDate">The publish date.</param>
    public void UpdateDetails(ISBN? isbn, string? description, string? author, string? publisher, string? language, DateOnly? publishDate)
    {
        ISBN = isbn;
        Description = description;
        Author = author;
        Publisher = publisher;
        Language = language;
        PublishDate = publishDate;
        MarkUpdated();
    }

    /// <summary>
    /// Updates additional book metadata.
    /// </summary>
    /// <param name="edition">The edition.</param>
    /// <param name="taxCategory">The tax category.</param>
    public void UpdateBookMetadata(string? edition, string? taxCategory)
    {
        Edition = edition;
        TaxCategory = taxCategory;
        MarkUpdated();
    }

    /// <summary>
    /// Updates inventory metadata.
    /// </summary>
    /// <param name="minimumStock">The minimum stock threshold.</param>
    /// <param name="shelfLocation">The shelf location.</param>
    /// <param name="imagePath">The image path.</param>
    public void UpdateInventoryMetadata(int minimumStock, string? shelfLocation, string? imagePath)
    {
        if (minimumStock < 0)
        {
            throw new ValidationException("Minimum stock cannot be negative.");
        }

        MinimumStock = minimumStock;
        ShelfLocation = shelfLocation;
        ImagePath = imagePath;
        MarkUpdated();
    }

    /// <summary>
    /// Sets the current stock quantity.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ValidationException("Quantity cannot be negative.");
        }

        Quantity = quantity;
        MarkUpdated();
    }

    /// <summary>
    /// Moves the product to another category.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    public void ChangeCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ValidationException("Category is required.");
        }

        CategoryId = categoryId;
        MarkUpdated();
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Product title is required.");
        }

        Title = title.Trim();
    }

    private static void ValidateMoney(decimal value, string propertyName)
    {
        if (value < 0)
        {
            throw new ValidationException($"{propertyName} cannot be negative.");
        }
    }
}
