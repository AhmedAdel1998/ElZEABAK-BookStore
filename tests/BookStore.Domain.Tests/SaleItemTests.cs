using BookStore.Domain.Entities;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Tests;

/// <summary>
/// Pins the per-line total math a sale's grand total is built from.
/// </summary>
public class SaleItemTests
{
    [Fact]
    public void Constructor_CalculatesTotalFromQuantityPriceAndDiscount()
    {
        var item = new SaleItem(Guid.NewGuid(), quantity: 3, unitPrice: 25m, discount: 10m);

        // (3 * 25) - 10 = 65.
        Assert.Equal(65m, item.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveQuantity(int quantity)
    {
        Assert.Throws<ValidationException>(() => new SaleItem(Guid.NewGuid(), quantity, 10m));
    }

    [Fact]
    public void Constructor_RejectsNegativeUnitPrice()
    {
        Assert.Throws<ValidationException>(() => new SaleItem(Guid.NewGuid(), 1, -0.01m));
    }

    [Fact]
    public void Constructor_RejectsNegativeDiscount()
    {
        Assert.Throws<ValidationException>(() => new SaleItem(Guid.NewGuid(), 1, 10m, discount: -1m));
    }

    [Fact]
    public void Constructor_ThrowsWhenDiscountExceedsLineValue()
    {
        // 1 * 10 - 20 would be negative; a line item can never carry a negative total.
        Assert.Throws<BusinessRuleException>(() => new SaleItem(Guid.NewGuid(), 1, 10m, discount: 20m));
    }

    [Fact]
    public void Constructor_DiscountEqualToLineValue_ProducesZeroTotal()
    {
        var item = new SaleItem(Guid.NewGuid(), 1, 10m, discount: 10m);

        Assert.Equal(0m, item.Total);
    }

    [Fact]
    public void UpdateQuantity_RecalculatesTotal()
    {
        var item = new SaleItem(Guid.NewGuid(), 1, 10m);

        item.UpdateQuantity(4);

        Assert.Equal(4, item.Quantity);
        Assert.Equal(40m, item.Total);
    }

    [Fact]
    public void UpdateQuantity_RejectsNonPositiveValue()
    {
        var item = new SaleItem(Guid.NewGuid(), 1, 10m);

        Assert.Throws<ValidationException>(() => item.UpdateQuantity(0));
    }

    [Fact]
    public void ApplyDiscount_RecalculatesTotal()
    {
        var item = new SaleItem(Guid.NewGuid(), 2, 10m);

        item.ApplyDiscount(5m);

        Assert.Equal(15m, item.Total);
    }

    [Fact]
    public void ApplyDiscount_ExceedingLineValue_Throws()
    {
        var item = new SaleItem(Guid.NewGuid(), 1, 10m);

        Assert.Throws<BusinessRuleException>(() => item.ApplyDiscount(11m));
    }
}
