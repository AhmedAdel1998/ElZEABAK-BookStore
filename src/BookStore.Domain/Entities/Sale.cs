using BookStore.Domain.Common;
using BookStore.Domain.Enums;
using BookStore.Domain.Events;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents a point-of-sale transaction.
/// </summary>
public class Sale : BaseEntity, IAggregateRoot
{
    private readonly List<SaleItem> _saleItems = [];

    private Sale()
    {
        InvoiceNumber = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sale"/> class.
    /// </summary>
    /// <param name="invoiceNumber">The invoice number.</param>
    /// <param name="userId">The cashier user identifier.</param>
    /// <param name="paymentMethod">The payment method.</param>
    public Sale(string invoiceNumber, Guid userId, PaymentMethod paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            throw new ValidationException("Invoice number is required.");
        }

        InvoiceNumber = invoiceNumber.Trim();
        UserId = userId;
        PaymentMethod = paymentMethod;
        SaleDate = DateTimeOffset.UtcNow;
        Status = SaleStatus.Pending;
    }

    /// <summary>
    /// Gets the invoice number.
    /// </summary>
    public string InvoiceNumber { get; private set; }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>
    /// Gets the customer.
    /// </summary>
    public Customer? Customer { get; private set; }

    /// <summary>
    /// Gets the cashier user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the cashier.
    /// </summary>
    public User? Cashier { get; private set; }

    /// <summary>
    /// Gets the sale date.
    /// </summary>
    public DateTimeOffset SaleDate { get; private set; }

    /// <summary>
    /// Gets the payment method.
    /// </summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>
    /// Gets the sale discount.
    /// </summary>
    public decimal Discount { get; private set; }

    /// <summary>
    /// Gets the sale tax.
    /// </summary>
    public decimal Tax { get; private set; }

    /// <summary>
    /// Gets the sale total.
    /// </summary>
    public decimal Total { get; private set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Tax"/> is already contained in the line prices.
    /// When it is, tax must not be added again when totalling the sale.
    /// </summary>
    public bool TaxInclusive { get; private set; }

    /// <summary>
    /// Gets the paid amount.
    /// </summary>
    public decimal PaidAmount { get; private set; }

    /// <summary>
    /// Gets the change amount.
    /// </summary>
    public decimal ChangeAmount { get; private set; }

    /// <summary>
    /// Gets the sale status.
    /// </summary>
    public SaleStatus Status { get; private set; }

    /// <summary>
    /// Gets sale items.
    /// </summary>
    public IReadOnlyCollection<SaleItem> SaleItems => _saleItems.AsReadOnly();

    /// <summary>
    /// Adds an item to the sale.
    /// </summary>
    /// <param name="item">The sale item.</param>
    public void AddItem(SaleItem item)
    {
        EnsurePending();
        _saleItems.Add(item);
        CalculateTotal();
        MarkUpdated();
    }

    /// <summary>
    /// Removes an item from the sale.
    /// </summary>
    /// <param name="saleItemId">The sale item identifier.</param>
    public void RemoveItem(Guid saleItemId)
    {
        EnsurePending();
        var item = _saleItems.FirstOrDefault(existing => existing.Id == saleItemId);
        if (item is null)
        {
            throw new NotFoundException("Sale item was not found.");
        }

        _saleItems.Remove(item);
        CalculateTotal();
        MarkUpdated();
    }

    /// <summary>
    /// Calculates the sale total.
    /// </summary>
    /// <returns>The calculated sale total.</returns>
    public decimal CalculateTotal()
    {
        var subtotal = _saleItems.Sum(item => item.CalculateTotal());

        // With tax-inclusive pricing the line prices already contain the tax, so adding Tax here
        // would charge it twice. Tax is still recorded for the receipt and the tax reports.
        var total = subtotal - Discount + (TaxInclusive ? 0m : Tax);
        if (total < 0)
        {
            throw new BusinessRuleException("Sale total cannot be negative.");
        }

        Total = total;
        ChangeAmount = PaidAmount > Total ? PaidAmount - Total : 0;
        return Total;
    }

    /// <summary>
    /// Cancels the sale.
    /// </summary>
    public void Cancel()
    {
        if (Status == SaleStatus.Completed)
        {
            throw new BusinessRuleException("Completed sales cannot be cancelled.");
        }

        Status = SaleStatus.Cancelled;
        MarkUpdated();
        AddDomainEvent(new SaleCancelled(Id));
    }

    /// <summary>
    /// Suspends the sale.
    /// </summary>
    public void Suspend()
    {
        EnsurePending();
        Status = SaleStatus.Suspended;
        MarkUpdated();
    }

    /// <summary>
    /// Completes the sale.
    /// </summary>
    /// <param name="paidAmount">The paid amount.</param>
    public void Complete(decimal paidAmount)
    {
        EnsurePending();

        if (_saleItems.Count == 0)
        {
            throw new BusinessRuleException("A sale must contain at least one item.");
        }

        if (paidAmount < Total)
        {
            throw new BusinessRuleException("Paid amount cannot be less than the total.");
        }

        PaidAmount = paidAmount;
        ChangeAmount = PaidAmount - Total;
        Status = SaleStatus.Completed;
        MarkUpdated();
        AddDomainEvent(new SaleCompleted(Id));
    }

    /// <summary>
    /// Updates the selected payment method.
    /// </summary>
    /// <param name="paymentMethod">The payment method.</param>
    public void UpdatePaymentMethod(PaymentMethod paymentMethod)
    {
        EnsurePending();
        PaymentMethod = paymentMethod;
        MarkUpdated();
    }

    /// <summary>
    /// Associates this sale with a customer or clears the customer for walk-in sales.
    /// </summary>
    /// <param name="customerId">The optional customer identifier.</param>
    public void AssignCustomer(Guid? customerId)
    {
        EnsurePending();
        CustomerId = customerId == Guid.Empty ? null : customerId;
        MarkUpdated();
    }

    /// <summary>
    /// Updates sale charges.
    /// </summary>
    /// <param name="discount">The sale discount.</param>
    /// <param name="tax">The sale tax.</param>
    public void UpdateCharges(decimal discount, decimal tax, bool taxInclusive = false)
    {
        EnsurePending();
        if (discount < 0 || tax < 0)
        {
            throw new ValidationException("Discount and tax cannot be negative.");
        }

        Discount = discount;
        Tax = tax;
        TaxInclusive = taxInclusive;
        CalculateTotal();
        MarkUpdated();
    }

    private void EnsurePending()
    {
        if (Status != SaleStatus.Pending)
        {
            throw new BusinessRuleException("Only pending sales can be changed.");
        }
    }
}
