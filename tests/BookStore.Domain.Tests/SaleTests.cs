using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Tests;

/// <summary>
/// Pins the money math and lifecycle rules a POS sale depends on. This is the domain's riskiest
/// untested surface before this suite existed: <see cref="BookStore.Application"/> handler tests
/// cover orchestration, but the total/discount/tax arithmetic and state transitions live here, in
/// the entity, and had no direct coverage.
/// </summary>
public class SaleTests
{
    private static Sale CreatePendingSale() => new("INV-0001", Guid.NewGuid(), PaymentMethod.Cash);

    [Fact]
    public void Constructor_RejectsBlankInvoiceNumber()
    {
        Assert.Throws<ValidationException>(() => new Sale("   ", Guid.NewGuid(), PaymentMethod.Cash));
    }

    [Fact]
    public void Constructor_TrimsInvoiceNumber()
    {
        var sale = new Sale("  INV-0042  ", Guid.NewGuid(), PaymentMethod.Cash);

        Assert.Equal("INV-0042", sale.InvoiceNumber);
        Assert.Equal(SaleStatus.Pending, sale.Status);
    }

    [Fact]
    public void AddItem_AccumulatesTotalAcrossMultipleLines()
    {
        var sale = CreatePendingSale();

        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 2, unitPrice: 50m));
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 30m, discount: 5m));

        // (2 * 50) + (1 * 30 - 5) = 125, no tax or invoice discount yet.
        Assert.Equal(125m, sale.Total);
    }

    [Fact]
    public void UpdateCharges_AppliesTaxAndDiscountToTotal()
    {
        var sale = CreatePendingSale();
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 100m));

        sale.UpdateCharges(discount: 10m, tax: 5m);

        // subtotal 100 + tax 5 - discount 10 = 95.
        Assert.Equal(95m, sale.Total);
    }

    [Fact]
    public void UpdateCharges_RejectsNegativeDiscountOrTax()
    {
        var sale = CreatePendingSale();
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 100m));

        Assert.Throws<ValidationException>(() => sale.UpdateCharges(discount: -1m, tax: 0m));
        Assert.Throws<ValidationException>(() => sale.UpdateCharges(discount: 0m, tax: -1m));
    }

    [Fact]
    public void UpdateCharges_ThrowsWhenDiscountExceedsSubtotalPlusTax()
    {
        var sale = CreatePendingSale();
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 10m));

        // A discount larger than subtotal + tax would make the invoice total negative.
        Assert.Throws<BusinessRuleException>(() => sale.UpdateCharges(discount: 50m, tax: 0m));
    }

    [Fact]
    public void RemoveItem_RecalculatesTotal()
    {
        var sale = CreatePendingSale();
        var keep = new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 20m);
        var drop = new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 80m);
        sale.AddItem(keep);
        sale.AddItem(drop);

        sale.RemoveItem(drop.Id);

        Assert.Equal(20m, sale.Total);
        Assert.Single(sale.SaleItems);
    }

    [Fact]
    public void RemoveItem_UnknownId_Throws()
    {
        var sale = CreatePendingSale();

        Assert.Throws<NotFoundException>(() => sale.RemoveItem(Guid.NewGuid()));
    }

    [Fact]
    public void Complete_RejectsEmptyCart()
    {
        var sale = CreatePendingSale();

        Assert.Throws<BusinessRuleException>(() => sale.Complete(0m));
    }

    [Fact]
    public void Complete_RejectsPaymentBelowTotal()
    {
        var sale = CreatePendingSale();
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 100m));

        Assert.Throws<BusinessRuleException>(() => sale.Complete(99.99m));
    }

    [Fact]
    public void Complete_CalculatesChangeAndTransitionsToCompleted()
    {
        var sale = CreatePendingSale();
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 100m));

        sale.Complete(150m);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(150m, sale.PaidAmount);
        Assert.Equal(50m, sale.ChangeAmount);
        Assert.Contains(sale.DomainEvents, e => e is Events.SaleCompleted);
    }

    [Fact]
    public void Complete_ExactPayment_ProducesZeroChange()
    {
        var sale = CreatePendingSale();
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 100m));

        sale.Complete(100m);

        Assert.Equal(0m, sale.ChangeAmount);
    }

    [Fact]
    public void Cancel_CompletedSale_Throws()
    {
        var sale = CreatePendingSale();
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 10m));
        sale.Complete(10m);

        Assert.Throws<BusinessRuleException>(sale.Cancel);
    }

    [Fact]
    public void Cancel_PendingSale_TransitionsAndRaisesEvent()
    {
        var sale = CreatePendingSale();

        sale.Cancel();

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
        Assert.Contains(sale.DomainEvents, e => e is Events.SaleCancelled);
    }

    [Theory]
    [InlineData(SaleStatus.Completed)]
    [InlineData(SaleStatus.Cancelled)]
    public void MutatingOperations_OnNonPendingSale_Throw(SaleStatus terminalStatus)
    {
        var sale = CreatePendingSale();
        sale.AddItem(new SaleItem(Guid.NewGuid(), quantity: 1, unitPrice: 10m));

        if (terminalStatus == SaleStatus.Completed)
        {
            sale.Complete(10m);
        }
        else
        {
            sale.Cancel();
        }

        // A sale that has left the Pending state is a closed transaction: none of its charges,
        // items, or payment method may change underneath a completed or cancelled invoice.
        Assert.Throws<BusinessRuleException>(() => sale.AddItem(new SaleItem(Guid.NewGuid(), 1, 5m)));
        Assert.Throws<BusinessRuleException>(() => sale.UpdateCharges(0m, 0m));
        Assert.Throws<BusinessRuleException>(() => sale.UpdatePaymentMethod(PaymentMethod.Card));
        Assert.Throws<BusinessRuleException>(() => sale.AssignCustomer(Guid.NewGuid()));
    }

    [Fact]
    public void AssignCustomer_EmptyGuid_ClearsCustomer()
    {
        var sale = CreatePendingSale();
        sale.AssignCustomer(Guid.NewGuid());

        sale.AssignCustomer(Guid.Empty);

        Assert.Null(sale.CustomerId);
    }
}
