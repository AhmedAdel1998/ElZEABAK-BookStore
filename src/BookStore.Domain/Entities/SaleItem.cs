using BookStore.Domain.Common;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents an item sold within a sale.
/// </summary>
public class SaleItem : BaseEntity
{
    private SaleItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleItem"/> class.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit price.</param>
    /// <param name="discount">The line discount.</param>
    /// <param name="unitCost">The product purchase cost captured at sale time.</param>
    public SaleItem(Guid productId, int quantity, decimal unitPrice, decimal discount = 0, decimal unitCost = 0)
    {
        if (quantity <= 0)
        {
            throw new ValidationException("Sale item quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new ValidationException("Sale item unit price cannot be negative.");
        }

        if (discount < 0)
        {
            throw new ValidationException("Sale item discount cannot be negative.");
        }

        if (unitCost < 0)
        {
            throw new ValidationException("Sale item unit cost cannot be negative.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitCost = unitCost;
        Discount = discount;
        Total = CalculateTotal();
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Gets the product.
    /// </summary>
    public Product? Product { get; private set; }

    /// <summary>
    /// Gets the quantity.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Gets the unit price.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Gets the purchase cost captured when the sale completed.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Gets the discount.
    /// </summary>
    public decimal Discount { get; private set; }

    /// <summary>
    /// Gets the line total.
    /// </summary>
    public decimal Total { get; private set; }

    /// <summary>
    /// Updates the sold quantity.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ValidationException("Sale item quantity must be greater than zero.");
        }

        Quantity = quantity;
        CalculateTotal();
        MarkUpdated();
    }

    /// <summary>
    /// Updates the line discount.
    /// </summary>
    /// <param name="discount">The discount.</param>
    public void ApplyDiscount(decimal discount)
    {
        if (discount < 0)
        {
            throw new ValidationException("Sale item discount cannot be negative.");
        }

        Discount = discount;
        CalculateTotal();
        MarkUpdated();
    }

    /// <summary>
    /// Calculates the line total.
    /// </summary>
    /// <returns>The calculated total.</returns>
    public decimal CalculateTotal()
    {
        var total = (UnitPrice * Quantity) - Discount;
        if (total < 0)
        {
            throw new BusinessRuleException("Sale item total cannot be negative.");
        }

        Total = total;
        return Total;
    }
}
