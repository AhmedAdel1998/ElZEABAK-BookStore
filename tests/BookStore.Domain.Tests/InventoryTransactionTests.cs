using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Tests;

/// <summary>
/// Pins the validation rules for the stock ledger entries produced by every sale, restock, and
/// manual adjustment.
/// </summary>
public class InventoryTransactionTests
{
    [Fact]
    public void SimpleConstructor_RejectsEmptyProductId()
    {
        Assert.Throws<ValidationException>(() =>
            new InventoryTransaction(Guid.Empty, 5, InventoryTransactionType.Purchase));
    }

    [Fact]
    public void SimpleConstructor_RejectsZeroQuantity()
    {
        Assert.Throws<ValidationException>(() =>
            new InventoryTransaction(Guid.NewGuid(), 0, InventoryTransactionType.Purchase));
    }

    [Fact]
    public void SimpleConstructor_RejectsNegativeQuantity()
    {
        // The simple overload forwards quantity as BOTH the transaction quantity and the ledger's
        // quantityAfter (see the chained constructor call), so a caller wanting to record a
        // deduction cannot pass a negative number here - it fails the "quantities cannot be
        // negative" ledger guard before the sign is ever interpreted as a direction. Callers
        // recording a decrease must use the ledger overload with an explicit quantityBefore and
        // quantityAfter instead. Pinned so this coupling is not "fixed" without updating callers.
        Assert.Throws<ValidationException>(() =>
            new InventoryTransaction(Guid.NewGuid(), -3, InventoryTransactionType.Sale));
    }

    [Fact]
    public void SimpleConstructor_AcceptsPositiveQuantity()
    {
        var transaction = new InventoryTransaction(Guid.NewGuid(), 3, InventoryTransactionType.Purchase);

        Assert.Equal(3, transaction.Quantity);
        Assert.Equal(3, transaction.QuantityAfter);
    }

    [Fact]
    public void LedgerConstructor_RejectsNegativeQuantityBeforeOrAfter()
    {
        Assert.Throws<ValidationException>(() =>
            new InventoryTransaction(Guid.NewGuid(), -1, InventoryTransactionType.Adjustment, quantityBefore: -1, quantityAfter: 5, reason: "Adjustment"));

        Assert.Throws<ValidationException>(() =>
            new InventoryTransaction(Guid.NewGuid(), -1, InventoryTransactionType.Adjustment, quantityBefore: 5, quantityAfter: -1, reason: "Adjustment"));
    }

    [Fact]
    public void LedgerConstructor_RejectsBlankReason()
    {
        Assert.Throws<ValidationException>(() =>
            new InventoryTransaction(Guid.NewGuid(), 5, InventoryTransactionType.Adjustment, quantityBefore: 0, quantityAfter: 5, reason: "   "));
    }

    [Fact]
    public void LedgerConstructor_TrimsReasonAndRetainsLedgerFields()
    {
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var transaction = new InventoryTransaction(
            productId,
            quantity: 5,
            transactionType: InventoryTransactionType.Purchase,
            quantityBefore: 10,
            quantityAfter: 15,
            reason: "  Restock from supplier  ",
            reference: "PO-100",
            userId: userId,
            userName: "cashier1",
            notes: "delivered on time");

        Assert.Equal(productId, transaction.ProductId);
        Assert.Equal(10, transaction.QuantityBefore);
        Assert.Equal(15, transaction.QuantityAfter);
        Assert.Equal("Restock from supplier", transaction.Reason);
        Assert.Equal("PO-100", transaction.Reference);
        Assert.Equal(userId, transaction.UserId);
    }
}
