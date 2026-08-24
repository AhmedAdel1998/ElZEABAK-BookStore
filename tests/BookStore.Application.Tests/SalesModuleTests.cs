using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Features.Barcode.Handlers;
using BookStore.Application.Features.Barcode.Queries.FindProductByBarcode;
using BookStore.Application.Features.Barcode.Validators;
using BookStore.Application.Features.Receipts.Commands;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Queries;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Application.Features.Sales.Commands.AddItem;
using BookStore.Application.Features.Sales.Commands.ApplyInvoiceDiscount;
using BookStore.Application.Features.Sales.Commands.ApplyLineDiscount;
using BookStore.Application.Features.Sales.Commands.CompleteSale;
using BookStore.Application.Features.Sales.Commands.RemoveItem;
using BookStore.Application.Features.Sales.Commands.ResumeSale;
using BookStore.Application.Features.Sales.Commands.StartSale;
using BookStore.Application.Features.Sales.Commands.SuspendSale;
using BookStore.Application.Features.Sales.Commands.UpdateItemQuantity;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Sales.Handlers;
using BookStore.Application.Features.Sales.Queries.GetOpenInvoices;
using BookStore.Application.Features.Sales.Commands.CancelSale;
using BookStore.Application.Features.Sales.Commands.CloseInvoice;
using BookStore.Application.Features.Sales.Commands.SaveInvoiceDraft;
using BookStore.Application.Features.Sales.Commands.SwitchInvoice;
using BookStore.Application.Features.Sales.Services;
using BookStore.Application.Features.Sales.Validators;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Domain.Specifications;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Constants;
using BookStore.Shared.Models;
using BookStore.Shared.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReceiptModel = BookStore.Application.Features.Receipts.DTOs.ReceiptModel;

namespace BookStore.Application.Tests;

public class SalesModuleTests
{
    [Fact]
    public async Task StartSale_CreatesCurrentSale()
    {
        var fixture = new Fixture();

        var result = await fixture.StartSale.HandleAsync(new StartSaleRequest());

        Assert.True(result.IsSuccess);
        Assert.StartsWith("POS-", result.Value!.InvoiceNumber);
        Assert.NotNull(await fixture.Store.GetCurrentAsync());
    }

    [Fact]
    public async Task AddItem_AddsProductWithoutDuplicateRows()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 10);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());

        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1));
        var result = await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(3, result.Value.Items[0].Quantity);
        Assert.Equal(7, result.Value.Items[0].AvailableQuantity);
    }

    [Fact]
    public async Task RemoveItem_RemovesProductFromCart()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 10);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var sale = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1))).Value!;

        var result = await fixture.RemoveItem.HandleAsync(new RemoveItemRequest(sale.Items[0].Id));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task BarcodeScan_CanAddFoundProductToCart()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct("BK100", quantity: 5);
        fixture.BarcodeService.Product = new BarcodeProductDto { ProductId = product.Id, Barcode = "BK100", Title = product.Title, Quantity = product.Quantity };
        var barcodeHandler = new FindProductByBarcodeHandler(fixture.BarcodeService, new FindProductByBarcodeRequestValidator(), NullLogger<FindProductByBarcodeHandler>.Instance);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());

        var lookup = await barcodeHandler.HandleAsync(new FindProductByBarcodeRequest("BK100"));
        var result = await fixture.AddItem.HandleAsync(new AddItemRequest(lookup.Value!.ProductId, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal("BK100", result.Value!.Items[0].Barcode);
        Assert.Equal(4, result.Value.Items[0].AvailableQuantity);
    }

    [Fact]
    public async Task UpdateQuantity_RecalculatesVisibleStockLeft()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 10);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var sale = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1))).Value!;

        var result = await fixture.UpdateQuantity.HandleAsync(new UpdateItemQuantityRequest(sale.Items[0].Id, 4));

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Items[0].Quantity);
        Assert.Equal(6, result.Value.Items[0].AvailableQuantity);
    }

    [Fact]
    public async Task UpdateQuantity_RejectsQuantityAboveInventory()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 2);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var sale = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1))).Value!;

        var result = await fixture.UpdateQuantity.HandleAsync(new UpdateItemQuantityRequest(sale.Items[0].Id, 3));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task InvoiceDiscount_RequiresPermission()
    {
        var fixture = new Fixture([]);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());

        var result = await fixture.ApplyInvoiceDiscount.HandleAsync(new ApplyInvoiceDiscountRequest(5));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LineDiscount_UpdatesSelectedLineAndTotals()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5, price: 100);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var sale = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2))).Value!;

        var result = await fixture.ApplyLineDiscount.HandleAsync(new ApplyLineDiscountRequest(sale.Items[0].Id, 25));

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value!.Items[0].Discount);
        Assert.Equal(200, result.Value.Summary.Subtotal);
        Assert.Equal(25, result.Value.Summary.LineDiscount);
        Assert.Equal(175, result.Value.Summary.GrandTotal);
    }

    [Fact]
    public async Task Pricing_CalculatesDiscountTaxAndGrandTotal()
    {
        var fixture = new Fixture();
        fixture.Settings.Value.Store.TaxRate = 0.14m;
        var product = fixture.AddProduct(quantity: 5, price: 100);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2));

        var result = await fixture.ApplyInvoiceDiscount.HandleAsync(new ApplyInvoiceDiscountRequest(20));

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Value!.Summary.Subtotal);

        // The invoice discount reduces the taxable base, so tax is 14% of 180 rather than of 200.
        // This previously expected 28.00/208.00, which charged tax on the discounted-away 20.
        Assert.Equal(25.20m, result.Value.Summary.Tax);
        Assert.Equal(205.20m, result.Value.Summary.GrandTotal);
    }

    [Fact]
    public async Task SuspendSale_MovesCurrentSaleToHeldSales()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1));

        var result = await fixture.SuspendSale.HandleAsync(new SuspendSaleRequest());

        Assert.True(result.IsSuccess);
        Assert.Null(await fixture.Store.GetCurrentAsync());
        Assert.Single(await fixture.Store.GetHeldAsync());
    }

    [Fact]
    public async Task StartSale_ResumesACartAlreadyInProgress()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var first = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 3))).Value!;

        // Navigating away from the POS and back constructs a new view model, which starts a sale.
        // That must not discard the cart the cashier is halfway through.
        var result = await fixture.StartSale.HandleAsync(new StartSaleRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(first.InvoiceNumber, result.Value!.InvoiceNumber);
        Assert.Single(result.Value.Items);
        Assert.Equal(3, result.Value.Items[0].Quantity);
    }

    [Fact]
    public async Task StartSale_WithForceNew_LeavesTheFirstInvoiceOpenAlongsideTheNewOne()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var first = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2))).Value!;

        var result = await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(first.InvoiceNumber, result.Value!.InvoiceNumber);
        Assert.Empty(result.Value.Items);

        // Both invoices are open, and the new one is the one on screen. Nothing was parked or lost.
        var open = await fixture.Store.GetOpenAsync();
        Assert.Equal(2, open.Count);
        Assert.Contains(open, sale => sale.SaleId == first.SaleId && sale.Items.Sum(item => item.Quantity) == 2);
        Assert.Empty(await fixture.Store.GetHeldAsync());
        Assert.Equal(result.Value.SaleId, (await fixture.Store.GetCurrentAsync())!.SaleId);
    }

    [Fact]
    public async Task StartSale_WithForceNew_ReusesAnUntouchedInvoiceInsteadOfStackingEmptyOnes()
    {
        var fixture = new Fixture();
        var first = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;

        var result = await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true));

        Assert.True(result.IsSuccess);
        Assert.Equal(first.SaleId, result.Value!.SaleId);
        Assert.Single(await fixture.Store.GetOpenAsync());
    }

    [Fact]
    public async Task StartSale_RefusesToOpenMoreThanTheInvoiceLimit()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: PosConstants.MaxOpenInvoices + 5);

        // Each invoice needs a line, otherwise the blank invoice is reused rather than added to.
        for (var index = 0; index < PosConstants.MaxOpenInvoices; index++)
        {
            var opened = await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true));
            Assert.True(opened.IsSuccess);
            await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, opened.Value!.SaleId));
        }

        var result = await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true));

        Assert.False(result.IsSuccess);
        Assert.Equal(PosConstants.MaxOpenInvoices, (await fixture.Store.GetOpenAsync()).Count);
    }

    [Fact]
    public async Task StartSale_StartsFreshWhenNothingIsInProgress()
    {
        var fixture = new Fixture();

        var result = await fixture.StartSale.HandleAsync(new StartSaleRequest());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task ResumeSale_LeavesTheInvoiceOnScreenOpenBesideTheResumedOne()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 10);

        // Hold a first sale.
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var parked = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1))).Value!;
        await fixture.SuspendSale.HandleAsync(new SuspendSaleRequest());

        // Build a second, larger cart, then resume the held one.
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var onScreen = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 4))).Value!;

        var result = await fixture.ResumeSale.HandleAsync(new ResumeSaleRequest(parked.SaleId));

        Assert.True(result.IsSuccess);
        Assert.Equal(parked.SaleId, result.Value!.SaleId);

        // Resuming no longer parks anything: both invoices are simply open, nothing is held.
        var open = await fixture.Store.GetOpenAsync();
        Assert.Equal(2, open.Count);
        Assert.Contains(open, sale => sale.SaleId == onScreen.SaleId && sale.Items.Sum(item => item.Quantity) == 4);
        Assert.Empty(await fixture.Store.GetHeldAsync());
    }

    [Fact]
    public async Task ResumeSale_RestoresHeldSaleAsCurrentSale()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var activeSale = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1))).Value!;
        await fixture.SuspendSale.HandleAsync(new SuspendSaleRequest());

        var result = await fixture.ResumeSale.HandleAsync(new ResumeSaleRequest(activeSale.SaleId));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsSuspended);
        Assert.Equal(activeSale.SaleId, result.Value.SaleId);
        Assert.Empty(await fixture.Store.GetHeldAsync());
        Assert.Equal(activeSale.SaleId, (await fixture.Store.GetCurrentAsync())!.SaleId);
    }

    [Fact]
    public async Task CompleteSale_PersistsSaleAndUpdatesInventory()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5, price: 100);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2));

        var result = await fixture.CompleteSale.HandleAsync(new CompleteSaleRequest(PaymentMethod.Cash, 200));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, product.Quantity);
        Assert.Single(fixture.InventoryRepository.Transactions);
        Assert.Single(fixture.SaleRepository.Sales);
        Assert.NotNull(fixture.ReceiptService.LastReceipt);
        Assert.Equal(1, fixture.ReceiptService.LastCopies);
    }

    [Fact]
    public async Task CompleteSale_RejectsPaidAmountBelowTotal()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5, price: 100);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2));

        var result = await fixture.CompleteSale.HandleAsync(new CompleteSaleRequest(PaymentMethod.Cash, 199));

        Assert.False(result.IsSuccess);
        Assert.Equal("Paid amount cannot be less than the total.", result.Error);
        Assert.Equal(5, product.Quantity);
        Assert.Empty(fixture.InventoryRepository.Transactions);
        Assert.Empty(fixture.SaleRepository.Sales);
    }

    [Fact]
    public async Task CompleteSale_PrintsRequestedReceiptCopies()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5, price: 100);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2));

        var result = await fixture.CompleteSale.HandleAsync(new CompleteSaleRequest(PaymentMethod.Cash, 200, ReceiptCopies: 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.ReceiptCopies);
        Assert.Equal(3, fixture.ReceiptService.LastCopies);
    }

    [Fact]
    public async Task CompleteSale_DoesNotRollbackWhenReceiptPrintingFailsAfterCommit()
    {
        var fixture = new Fixture();
        fixture.ReceiptService.ThrowOnPrint = true;
        var product = fixture.AddProduct(quantity: 5, price: 100);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2));

        var result = await fixture.CompleteSale.HandleAsync(new CompleteSaleRequest(PaymentMethod.Cash, 200));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.ReceiptPrintSucceeded);
        // The message describes only the printing fault. The POS notification wraps it in
        // "Sale completed, but receipt printing failed. {0}", so repeating that clause here
        // showed the cashier the same sentence twice.
        Assert.Equal("Receipt printing failed unexpectedly.", result.Value.ReceiptPrintError);
        Assert.Equal(3, product.Quantity);
        Assert.Single(fixture.InventoryRepository.Transactions);
        Assert.Single(fixture.SaleRepository.Sales);
        Assert.Null(await fixture.Store.GetCurrentAsync());
        Assert.False(fixture.UnitOfWork.RollbackCalled);
    }

    [Fact]
    public async Task CompleteSale_RollsBackWhenInventoryIsInsufficient()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 1, price: 100);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());
        var sale = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1))).Value!;
        sale.Items[0].AvailableQuantity = 5;
        sale.Items[0].Quantity = 2;
        await fixture.Store.SaveCurrentAsync(sale);

        var result = await fixture.CompleteSale.HandleAsync(new CompleteSaleRequest(PaymentMethod.Cash, 200));

        Assert.False(result.IsSuccess);
        Assert.True(fixture.UnitOfWork.RollbackCalled);
        Assert.Equal(1, product.Quantity);
    }

    [Fact]
    public async Task CompleteSale_RejectsOverlappingCheckout()
    {
        var fixture = new Fixture();
        fixture.CheckoutGuard.IsLocked = true;

        var result = await fixture.CompleteSale.HandleAsync(new CompleteSaleRequest(PaymentMethod.Cash, 200));

        Assert.False(result.IsSuccess);
        Assert.Equal("Checkout is already in progress.", result.Error);
        Assert.Empty(fixture.SaleRepository.Sales);
    }

    [Fact]
    public async Task TwoOpenInvoices_KeepSeparateLinesCustomersAndTotals()
    {
        var fixture = new Fixture();
        var pen = fixture.AddProduct("BK-PEN", quantity: 10, price: 30);
        var book = fixture.AddProduct("BK-BOOK", quantity: 10, price: 100);

        var first = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(pen.Id, 2, first.SaleId));

        var second = (await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true))).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(book.Id, 1, second.SaleId));

        // Adding to the invoice that is not on screen must land on that invoice, not the active one.
        var updatedFirst = (await fixture.AddItem.HandleAsync(new AddItemRequest(pen.Id, 1, first.SaleId))).Value!;

        Assert.Equal(3, Assert.Single(updatedFirst.Items).Quantity);
        Assert.Equal(90, updatedFirst.Summary.GrandTotal);

        var reloadedSecond = await fixture.Store.GetAsync(second.SaleId);
        Assert.Equal(1, Assert.Single(reloadedSecond!.Items).Quantity);
        Assert.Equal(100, reloadedSecond.Summary.GrandTotal);
    }

    [Fact]
    public async Task OpenInvoices_DoNotSellTheSameUnitTwice()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5);

        var first = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        var loaded = await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 4, first.SaleId));
        Assert.True(loaded.IsSuccess);

        var second = (await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true))).Value!;

        // Only one unit is left over from the first invoice, so a second unit must be refused.
        var takesLast = await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, second.SaleId));
        var overSells = await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, second.SaleId));

        Assert.True(takesLast.IsSuccess);
        Assert.False(overSells.IsSuccess);
        Assert.Equal(0, takesLast.Value!.Items[0].AvailableQuantity);
    }

    [Fact]
    public async Task UpdateQuantity_CannotClaimStockAnotherOpenInvoiceIsHolding()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 6);

        var first = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        var firstLine = (await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, first.SaleId))).Value!.Items[0];

        var second = (await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true))).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 4, second.SaleId));

        // Six in stock, four held by the other invoice, so this invoice tops out at two.
        var allowed = await fixture.UpdateQuantity.HandleAsync(new UpdateItemQuantityRequest(firstLine.Id, 2, first.SaleId));
        var refused = await fixture.UpdateQuantity.HandleAsync(new UpdateItemQuantityRequest(firstLine.Id, 3, first.SaleId));

        Assert.True(allowed.IsSuccess);
        Assert.False(refused.IsSuccess);
    }

    [Fact]
    public async Task SwitchInvoice_MakesTheNamedInvoiceActiveAndReconcilesItsStock()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 10);

        var first = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 3, first.SaleId));
        var second = (await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true))).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, second.SaleId));

        // Stock is written down behind the cashier's back while the first invoice sits idle.
        product.SetQuantity(2);

        var result = await fixture.SwitchInvoice.HandleAsync(new SwitchInvoiceRequest(first.SaleId));

        Assert.True(result.IsSuccess);
        Assert.Equal(first.SaleId, (await fixture.Store.GetCurrentAsync())!.SaleId);

        // Two left, one of them held by the other invoice, so this line is cut back to one.
        Assert.Equal(1, Assert.Single(result.Value!.Items).Quantity);
    }

    [Fact]
    public async Task SwitchInvoice_RejectsAnInvoiceThatIsNotOpen()
    {
        var fixture = new Fixture();
        await fixture.StartSale.HandleAsync(new StartSaleRequest());

        var result = await fixture.SwitchInvoice.HandleAsync(new SwitchInvoiceRequest(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CompleteSale_ClosesOnlyThePaidInvoice()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 10, price: 100);

        var paying = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, paying.SaleId));
        var waiting = (await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true))).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2, waiting.SaleId));

        var result = await fixture.CompleteSale.HandleAsync(new CompleteSaleRequest(PaymentMethod.Cash, 100, SaleId: paying.SaleId));

        Assert.True(result.IsSuccess);
        Assert.Equal(paying.InvoiceNumber, result.Value!.InvoiceNumber);

        // Only one unit came off stock, and the other invoice is untouched and still open.
        Assert.Equal(9, product.Quantity);
        var open = await fixture.Store.GetOpenAsync();
        Assert.Equal(waiting.SaleId, Assert.Single(open).SaleId);
        Assert.Equal(2, open.Single().Items.Sum(item => item.Quantity));
    }

    [Fact]
    public async Task CloseInvoice_DiscardsOneInvoiceAndActivatesAnother()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 10);

        var keep = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, keep.SaleId));
        var discard = (await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true))).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2, discard.SaleId));

        var result = await fixture.CloseInvoice.HandleAsync(new CloseInvoiceRequest(discard.SaleId));

        Assert.True(result.IsSuccess);
        Assert.Equal(keep.SaleId, Assert.Single(await fixture.Store.GetOpenAsync()).SaleId);
        Assert.Equal(keep.SaleId, (await fixture.Store.GetCurrentAsync())!.SaleId);

        // The two units the closed invoice held are free again; the one the surviving invoice holds is not.
        Assert.Equal(9, await fixture.CartStock.GetAvailableAsync(product.Id, discard.SaleId));
    }

    [Fact]
    public async Task CloseInvoice_WithItems_RequiresCancelPermission()
    {
        var fixture = new Fixture([PermissionConstants.SalesComplete]);
        var product = fixture.AddProduct(quantity: 5);
        var invoice = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, invoice.SaleId));

        var result = await fixture.CloseInvoice.HandleAsync(new CloseInvoiceRequest(invoice.SaleId));

        Assert.False(result.IsSuccess);
        Assert.Single(await fixture.Store.GetOpenAsync());
    }

    [Fact]
    public async Task CloseInvoice_WithoutItems_NeedsNoCancelPermission()
    {
        var fixture = new Fixture([]);
        var invoice = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;

        var result = await fixture.CloseInvoice.HandleAsync(new CloseInvoiceRequest(invoice.SaleId));

        Assert.True(result.IsSuccess);
        Assert.Empty(await fixture.Store.GetOpenAsync());
    }

    [Fact]
    public async Task SaveInvoiceDraft_KeepsThePaymentBoxOfAnInvoiceLeftForLater()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5, price: 100);
        var first = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, first.SaleId));

        await fixture.SaveDraft.HandleAsync(new SaveInvoiceDraftRequest(PaymentMethod.Card, 150, 3, first.SaleId));
        await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true));

        var reloaded = await fixture.Store.GetAsync(first.SaleId);

        Assert.Equal(PaymentMethod.Card, reloaded!.PaymentMethod);
        Assert.Equal(150, reloaded.AmountPaid);
        Assert.Equal(3, reloaded.ReceiptCopies);
        Assert.Equal(50, reloaded.Summary.Change);
    }

    [Fact]
    public async Task GetOpenInvoices_ListsEveryInvoiceOnTheWorkstation()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 10);
        var first = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, first.SaleId));
        var second = (await fixture.StartSale.HandleAsync(new StartSaleRequest(ForceNew: true))).Value!;

        var result = await fixture.GetOpenInvoices.HandleAsync(new GetOpenInvoicesRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, invoice => invoice.SaleId == first.SaleId);
        Assert.Contains(result.Value, invoice => invoice.SaleId == second.SaleId);
    }

    [Fact]
    public async Task AddItem_FailsClearlyWhenNoInvoiceIsOpen()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5);

        var result = await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("There is no open invoice.", result.Error);
        Assert.Empty(await fixture.Store.GetOpenAsync());
    }

    [Fact]
    public async Task AddItem_RejectsAnInvoiceThatIsNotOpen()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5);
        await fixture.StartSale.HandleAsync(new StartSaleRequest());

        var result = await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 1, Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal("That invoice is not open.", result.Error);
    }

    [Fact]
    public async Task CancelSale_FailsWhenThereIsNothingToCancel()
    {
        var fixture = new Fixture();

        var result = await fixture.CancelSale.HandleAsync(new CancelSaleRequest("No reason"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CartStock_DropsALineWhoseProductWentAway()
    {
        var fixture = new Fixture();
        var product = fixture.AddProduct(quantity: 5);
        var invoice = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;
        await fixture.AddItem.HandleAsync(new AddItemRequest(product.Id, 2, invoice.SaleId));

        product.Deactivate();

        var reloaded = (await fixture.StartSale.HandleAsync(new StartSaleRequest())).Value!;

        Assert.Empty(reloaded.Items);
        Assert.Equal(0, reloaded.Summary.GrandTotal);
    }

    private sealed class Fixture
    {
        public Fixture(IReadOnlyCollection<string>? permissions = null)
        {
            Authorization = new FakeAuthorizationService(permissions ?? [PermissionConstants.SalesCancel, PermissionConstants.SalesSuspend, PermissionConstants.SalesComplete, PermissionConstants.SalesApplyDiscount]);
            UnitOfWork = new FakeUnitOfWork(ProductRepository, InventoryRepository, SaleRepository);
            Pricing = new PricingService(new FakeSettingsService(Settings.Value));
            CartStock = new PosCartStockService(UnitOfWork, Store);
            StartSale = new StartSaleHandler(CurrentUser, Store, CartStock, Pricing, new StartSaleRequestValidator(), NullLogger<StartSaleHandler>.Instance);
            AddItem = new AddItemHandler(UnitOfWork, Store, CartStock, Pricing, new AddItemRequestValidator());
            UpdateQuantity = new UpdateItemQuantityHandler(Store, CartStock, Pricing, new UpdateItemQuantityRequestValidator());
            RemoveItem = new RemoveItemHandler(Store, Pricing, new RemoveItemRequestValidator());
            ApplyLineDiscount = new ApplyLineDiscountHandler(Authorization, Store, Pricing, new ApplyLineDiscountRequestValidator());
            ApplyInvoiceDiscount = new ApplyInvoiceDiscountHandler(Authorization, Store, Pricing, new ApplyInvoiceDiscountRequestValidator());
            SuspendSale = new SuspendSaleHandler(Authorization, Store);
            CancelSale = new CancelSaleHandler(Authorization, Store, new CancelSaleRequestValidator(), NullLogger<CancelSaleHandler>.Instance);
            ResumeSale = new ResumeSaleHandler(Store, CartStock, Pricing, new ResumeSaleRequestValidator());
            SwitchInvoice = new SwitchInvoiceHandler(Store, CartStock, Pricing, NullLogger<SwitchInvoiceHandler>.Instance);
            CloseInvoice = new CloseInvoiceHandler(Authorization, Store, NullLogger<CloseInvoiceHandler>.Instance);
            SaveDraft = new SaveInvoiceDraftHandler(Store, Pricing);
            GetOpenInvoices = new GetOpenInvoicesHandler(Store);
            CheckoutGuard = new FakeCheckoutConcurrencyGuard();
            CompleteSale = new CompleteSaleHandler(Authorization, CurrentUser, UnitOfWork, Store, Pricing, ReceiptService, CheckoutGuard, new CompleteSaleRequestValidator(), NullLogger<CompleteSaleHandler>.Instance);
        }

        public OptionsWrapper<ApplicationSettings> Settings { get; } = new(new ApplicationSettings());
        public FakeCurrentUserService CurrentUser { get; } = new();
        public FakeAuthorizationService Authorization { get; }
        public InMemoryPosSaleSessionStore Store { get; } = new();
        public FakeProductRepository ProductRepository { get; } = new();
        public FakeInventoryRepository InventoryRepository { get; } = new();
        public FakeSaleRepository SaleRepository { get; } = new();
        public FakeReceiptService ReceiptService { get; } = new();
        public FakeBarcodeService BarcodeService { get; } = new();
        public FakeCheckoutConcurrencyGuard CheckoutGuard { get; }
        public PricingService Pricing { get; }
        public PosCartStockService CartStock { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public StartSaleHandler StartSale { get; }
        public AddItemHandler AddItem { get; }
        public UpdateItemQuantityHandler UpdateQuantity { get; }
        public RemoveItemHandler RemoveItem { get; }
        public ApplyLineDiscountHandler ApplyLineDiscount { get; }
        public ApplyInvoiceDiscountHandler ApplyInvoiceDiscount { get; }
        public SuspendSaleHandler SuspendSale { get; }
        public CancelSaleHandler CancelSale { get; }
        public ResumeSaleHandler ResumeSale { get; }
        public SwitchInvoiceHandler SwitchInvoice { get; }
        public CloseInvoiceHandler CloseInvoice { get; }
        public SaveInvoiceDraftHandler SaveDraft { get; }
        public GetOpenInvoicesHandler GetOpenInvoices { get; }
        public CompleteSaleHandler CompleteSale { get; }

        public Product AddProduct(string barcode = "BK100", int quantity = 5, decimal price = 50)
        {
            var product = new Product(new Barcode(barcode), "Clean Architecture", 20, price, Guid.NewGuid());
            product.SetQuantity(quantity);
            ProductRepository.Products.Add(product);
            return product;
        }
    }

    private sealed class FakeUnitOfWork(FakeProductRepository products, FakeInventoryRepository inventory, FakeSaleRepository sales) : IUnitOfWork
    {
        public bool RollbackCalled { get; private set; }
        public IProductRepository Products => products;
        public ICategoryRepository Categories => null!;
        public ICustomerRepository Customers => null!;
        public ISupplierRepository Suppliers => null!;
        public IUserRepository Users => null!;
        public IRoleRepository Roles => null!;
        public ISaleRepository Sales => sales;
        public IInventoryRepository Inventory => inventory;
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) { RollbackCalled = true; return Task.CompletedTask; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public List<Product> Products { get; } = [];
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Products.FirstOrDefault(product => product.Id == id));
        public Task<IReadOnlyCollection<Product>> ListAsync(ISpecification<Product>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Product>>(Products);
        public Task AddAsync(Product product, CancellationToken cancellationToken = default) { Products.Add(product); return Task.CompletedTask; }
        public void Remove(Product product) => Products.Remove(product);
        public Task<IReadOnlyCollection<Product>> SearchAsync(string? searchTerm, bool? isActive, bool lowStockOnly, Guid? categoryId, decimal? minPrice, decimal? maxPrice, int? minQuantity, int? maxQuantity, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = Products.Where(product => isActive is null || product.IsActive == isActive);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(product => product.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || product.Barcode.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || (product.Author?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) || (product.ISBN?.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return Task.FromResult<IReadOnlyCollection<Product>>(query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray());
        }

        public Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, bool lowStockOnly = false, Guid? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null, int? minQuantity = null, int? maxQuantity = null, CancellationToken cancellationToken = default) => Task.FromResult(Products.Count);
        public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludedProductId = null, CancellationToken cancellationToken = default) => Task.FromResult(Products.Any(product => product.Barcode.Value == barcode && product.Id != excludedProductId));
        public Task<bool> ExistsByIsbnAsync(string isbn, Guid? excludedProductId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasCompletedSaleReferencesAsync(Guid productId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(Products.FirstOrDefault(product => product.Barcode.Value == barcode));
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        public List<InventoryTransaction> Transactions { get; } = [];
        public Task<InventoryTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.FirstOrDefault(transaction => transaction.Id == id));
        public Task<IReadOnlyCollection<InventoryTransaction>> ListAsync(ISpecification<InventoryTransaction>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<InventoryTransaction>>(Transactions);
        public Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default) { Transactions.Add(transaction); return Task.CompletedTask; }
        public Task<IReadOnlyCollection<InventoryTransaction>> SearchHistoryAsync(Guid? productId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, InventoryTransactionType? transactionType, Guid? userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<InventoryTransaction>>(Transactions);
        public Task<int> CountHistoryAsync(Guid? productId = null, DateTimeOffset? dateFrom = null, DateTimeOffset? dateTo = null, InventoryTransactionType? transactionType = null, Guid? userId = null, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.Count);
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        public List<Sale> Sales { get; } = [];
        public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Sales.FirstOrDefault(sale => sale.Id == id));
        public Task<Sale?> GetCompletedWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Sales.FirstOrDefault(sale => sale.Id == id && sale.Status == SaleStatus.Completed));
        public Task<Sale?> GetCompletedByInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default) => Task.FromResult(Sales.FirstOrDefault(sale => sale.InvoiceNumber == invoiceNumber && sale.Status == SaleStatus.Completed));
        public Task<IReadOnlyCollection<Sale>> ListAsync(ISpecification<Sale>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Sale>>(Sales);
        public Task<IReadOnlyCollection<Sale>> SearchCompletedAsync(string? invoiceNumber, DateTimeOffset? date, Guid? cashierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Sale>>(Sales.Where(sale => sale.Status == SaleStatus.Completed).ToArray());
        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) { Sales.Add(sale); return Task.CompletedTask; }
    }

    private sealed class FakeReceiptService : IReceiptService
    {
        public ReceiptModel? LastReceipt { get; private set; }
        public int LastCopies { get; private set; }
        public bool ThrowOnPrint { get; set; }
        public Task<Result<ReceiptModel>> BuildReceiptAsync(Guid saleId, CancellationToken cancellationToken = default) => Task.FromResult(Result<ReceiptModel>.Success(LastReceipt ?? new ReceiptModel { SaleId = saleId, InvoiceNumber = "TEST" }));
        public Task<Result<ReceiptModel>> BuildReceiptByInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default) => Task.FromResult(Result<ReceiptModel>.Success(LastReceipt ?? new ReceiptModel { InvoiceNumber = invoiceNumber }));
        public Task<ReceiptPrintResult> PrintCompletedSaleAsync(Guid saleId, PrintRequestKind kind, string? printerName = null, int copies = 1, CancellationToken cancellationToken = default)
        {
            if (ThrowOnPrint)
            {
                throw new InvalidOperationException("Printer driver failed.");
            }

            LastCopies = copies;
            LastReceipt = new ReceiptModel { SaleId = saleId, InvoiceNumber = "TEST" };
            return Task.FromResult(ReceiptPrintResult.Success(LastReceipt.PrintRequestId, printerName, LastReceipt));
        }

        public Task<ReceiptPrintResult> ReprintAsync(ReprintReceiptCommand command, CancellationToken cancellationToken = default) => Task.FromResult(ReceiptPrintResult.Success(Guid.NewGuid(), command.PrinterName));
        public Task<ReceiptPrintResult> TestPrintAsync(TestPrintCommand command, CancellationToken cancellationToken = default) => Task.FromResult(ReceiptPrintResult.Success(Guid.NewGuid(), command.PrinterName));
        public Task<Result<ReceiptPreviewDto>> PreviewAsync(GetReceiptPreviewQuery query, CancellationToken cancellationToken = default) => Task.FromResult(Result<ReceiptPreviewDto>.Success(new ReceiptPreviewDto()));
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public string? Username => "cashier";
        public string? FullName => "Cashier";
        public string? Role => "Cashier";
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? SessionId => Guid.NewGuid();
        public DateTimeOffset? LoginTime => DateTimeOffset.UtcNow;
        public void SignIn(UserSessionSnapshot session) { }
        public void SignOut() { }
    }

    private sealed class FakeAuthorizationService(IReadOnlyCollection<string> permissions) : IAuthorizationService
    {
        public bool HasPermission(string permission) => permissions.Contains(permission);
        public bool HasPermissions(params string[] permissionsToCheck) => permissionsToCheck.All(HasPermission);
        public bool HasRole(string role) => false;
        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
    }

    private sealed class FakeBarcodeService : IBarcodeService
    {
        public BarcodeProductDto? Product { get; set; }
        public Task<BarcodeDto> GenerateUniqueAsync(BarcodeFormat format, string? prefix = null, CancellationToken cancellationToken = default) => Task.FromResult(new BarcodeDto());
        public bool IsValid(string barcode, BarcodeFormat format) => true;
        public Task<bool> IsDuplicateAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ReserveAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public string GenerateImageSvg(string barcode, BarcodeFormat format) => string.Empty;
        public Task<BarcodeProductDto?> FindProductAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(Product);
    }

    private sealed class FakeCheckoutConcurrencyGuard : ICheckoutConcurrencyGuard
    {
        public bool IsLocked { get; set; }

        public Task<bool> TryEnterAsync(CancellationToken cancellationToken = default)
        {
            if (IsLocked)
            {
                return Task.FromResult(false);
            }

            IsLocked = true;
            return Task.FromResult(true);
        }

        public void Exit()
        {
            IsLocked = false;
        }
    }

    private sealed class FakeSettingsService(ApplicationSettings settings) : ISettingsService
    {
        public Task<T> GetAsync<T>(CancellationToken cancellationToken = default)
            where T : class, new()
        {
            object value = typeof(T) == typeof(TaxSettingsDto)
                ? new TaxSettingsDto { Enabled = settings.Store.TaxRate > 0, DefaultRate = settings.Store.TaxRate }
                : new T();
            return Task.FromResult((T)value);
        }

        public Task<Result> SetAsync<T>(T settings, CancellationToken cancellationToken = default)
            where T : class, new() => Task.FromResult(Result.Success());

        public Task<IReadOnlyList<SettingEntryDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SettingEntryDto>>([]);
        public Task<Result> ResetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task<Result> ResetCategoryAsync(SettingsCategory category, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
