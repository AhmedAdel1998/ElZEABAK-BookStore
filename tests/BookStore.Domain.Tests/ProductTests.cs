using BookStore.Domain.Entities;
using BookStore.Domain.Events;
using BookStore.Domain.Exceptions;
using BookStore.Domain.ValueObjects;

namespace BookStore.Domain.Tests;

/// <summary>
/// Pins the stock and pricing invariants a product's inventory and checkout behavior rely on.
/// </summary>
public class ProductTests
{
    private static Product CreateProduct(decimal purchasePrice = 10m, decimal sellingPrice = 20m) =>
        new(new Barcode("ABC-123456"), "The Time Machine", purchasePrice, sellingPrice, Guid.NewGuid());

    [Fact]
    public void Constructor_TrimsTitleAndStartsAtZeroStock()
    {
        var product = new Product(new Barcode("ABC-123456"), "  Dune  ", 10m, 20m, Guid.NewGuid());

        Assert.Equal("Dune", product.Title);
        Assert.Equal(0, product.Quantity);
        Assert.True(product.IsActive);
        Assert.Contains(product.DomainEvents, e => e is ProductCreated);
    }

    [Fact]
    public void Constructor_RejectsBlankTitle()
    {
        Assert.Throws<ValidationException>(() => new Product(new Barcode("ABC-123456"), "   ", 10m, 20m, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(-1, 20)]
    [InlineData(10, -1)]
    public void Constructor_RejectsNegativePrices(decimal purchasePrice, decimal sellingPrice)
    {
        Assert.Throws<ValidationException>(() => new Product(new Barcode("ABC-123456"), "Book", purchasePrice, sellingPrice, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_DoesNotEnforceSellingPriceAboveCost()
    {
        // Selling below cost is a data-quality warning (see DataQualityService), not a domain
        // invariant - this pins that intentional distinction so it is not "fixed" accidentally.
        var product = new Product(new Barcode("ABC-123456"), "Clearance Book", purchasePrice: 50m, sellingPrice: 10m, Guid.NewGuid());

        Assert.Equal(50m, product.PurchasePrice);
        Assert.Equal(10m, product.SellingPrice);
    }

    [Fact]
    public void IncreaseStock_AddsQuantityAndRaisesEvent()
    {
        var product = CreateProduct();

        product.IncreaseStock(5);

        Assert.Equal(5, product.Quantity);
        Assert.Contains(product.DomainEvents, e => e is ProductStockChanged changed && changed.Quantity == 5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IncreaseStock_RejectsNonPositiveQuantity(int quantity)
    {
        var product = CreateProduct();

        Assert.Throws<BusinessRuleException>(() => product.IncreaseStock(quantity));
    }

    [Fact]
    public void DecreaseStock_ReducesQuantity()
    {
        var product = CreateProduct();
        product.IncreaseStock(10);

        product.DecreaseStock(4);

        Assert.Equal(6, product.Quantity);
    }

    [Fact]
    public void DecreaseStock_BelowZero_Throws()
    {
        var product = CreateProduct();
        product.IncreaseStock(3);

        // This is the guard that keeps a POS sale from selling more units than are on the shelf.
        Assert.Throws<BusinessRuleException>(() => product.DecreaseStock(4));
    }

    [Fact]
    public void DecreaseStock_ExactlyToZero_Succeeds()
    {
        var product = CreateProduct();
        product.IncreaseStock(3);

        product.DecreaseStock(3);

        Assert.Equal(0, product.Quantity);
    }

    [Fact]
    public void SetQuantity_RejectsNegativeValue()
    {
        var product = CreateProduct();

        Assert.Throws<ValidationException>(() => product.SetQuantity(-1));
    }

    [Fact]
    public void UpdatePrice_RejectsNegativeValues()
    {
        var product = CreateProduct();

        Assert.Throws<ValidationException>(() => product.UpdatePrice(-1m, 20m));
        Assert.Throws<ValidationException>(() => product.UpdatePrice(10m, -1m));
    }

    [Fact]
    public void UpdateBarcode_NullThrows()
    {
        var product = CreateProduct();

        Assert.Throws<ValidationException>(() => product.UpdateBarcode(null!));
    }

    [Fact]
    public void ChangeCategory_RejectsEmptyGuid()
    {
        var product = CreateProduct();

        Assert.Throws<ValidationException>(() => product.ChangeCategory(Guid.Empty));
    }

    [Fact]
    public void UpdateInventoryMetadata_RejectsNegativeMinimumStock()
    {
        var product = CreateProduct();

        Assert.Throws<ValidationException>(() => product.UpdateInventoryMetadata(-1, null, null));
    }

    [Fact]
    public void ActivateAndDeactivate_ToggleIsActive()
    {
        var product = CreateProduct();

        product.Deactivate();
        Assert.False(product.IsActive);

        product.Activate();
        Assert.True(product.IsActive);
    }
}
