using BookStore.Domain.Common;
using BookStore.Domain.Enums;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents a product inventory transaction.
/// </summary>
public class InventoryTransaction : BaseEntity, IAggregateRoot
{
    private InventoryTransaction()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryTransaction"/> class.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The transaction quantity.</param>
    /// <param name="transactionType">The transaction type.</param>
    /// <param name="reference">The transaction reference.</param>
    /// <param name="notes">The transaction notes.</param>
    public InventoryTransaction(Guid productId, int quantity, InventoryTransactionType transactionType, string? reference = null, string? notes = null)
        : this(productId, quantity, transactionType, 0, quantity, "Inventory movement", reference, null, null, notes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryTransaction"/> class with ledger details.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="quantity">The signed quantity change.</param>
    /// <param name="transactionType">The transaction type.</param>
    /// <param name="quantityBefore">The quantity before the movement.</param>
    /// <param name="quantityAfter">The quantity after the movement.</param>
    /// <param name="reason">The adjustment reason.</param>
    /// <param name="reference">The transaction reference.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="userName">The user display name.</param>
    /// <param name="notes">The transaction notes.</param>
    public InventoryTransaction(
        Guid productId,
        int quantity,
        InventoryTransactionType transactionType,
        int quantityBefore,
        int quantityAfter,
        string reason,
        string? reference = null,
        Guid? userId = null,
        string? userName = null,
        string? notes = null)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException("Product is required.");
        }

        if (quantity == 0)
        {
            throw new ValidationException("Inventory transaction quantity cannot be zero.");
        }

        if (quantityBefore < 0 || quantityAfter < 0)
        {
            throw new ValidationException("Inventory quantities cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("Inventory transaction reason is required.");
        }

        ProductId = productId;
        Quantity = quantity;
        TransactionType = transactionType;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        Reason = reason.Trim();
        Reference = reference;
        UserId = userId;
        UserName = userName;
        Notes = notes;
        Date = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Gets the transaction quantity.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Gets the transaction type.
    /// </summary>
    public InventoryTransactionType TransactionType { get; private set; }

    /// <summary>
    /// Gets the product quantity before the movement.
    /// </summary>
    public int QuantityBefore { get; private set; }

    /// <summary>
    /// Gets the product quantity after the movement.
    /// </summary>
    public int QuantityAfter { get; private set; }

    /// <summary>
    /// Gets the movement reason.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the reference.
    /// </summary>
    public string? Reference { get; private set; }

    /// <summary>
    /// Gets the user identifier that created the transaction.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the user display name that created the transaction.
    /// </summary>
    public string? UserName { get; private set; }

    /// <summary>
    /// Gets transaction notes.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Gets the transaction date.
    /// </summary>
    public DateTimeOffset Date { get; private set; }
}
