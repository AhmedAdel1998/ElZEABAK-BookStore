using System.Text.Json;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Domain.Enums;
using BookStore.Infrastructure.Sales;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Infrastructure.Tests;

/// <summary>
/// Covers the on-disk POS session store: several invoices open at once, which one is active, the
/// held set, and what happens to a cart file that a crash left unreadable.
/// </summary>
public class PosSaleSessionStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly PosSaleSessionStore _store;

    public PosSaleSessionStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "bookstore-pos-session-tests", Guid.NewGuid().ToString("N"));
        _store = new PosSaleSessionStore(NullLogger<PosSaleSessionStore>.Instance, _directory);
    }

    [Fact]
    public async Task GetCurrent_IsNullBeforeAnythingIsOpened()
    {
        Assert.Null(await _store.GetCurrentAsync());
        Assert.Empty(await _store.GetOpenAsync());
        Assert.Empty(await _store.GetHeldAsync());
    }

    [Fact]
    public async Task SaveCurrent_KeepsEveryInvoiceOpenAndTracksTheActiveOne()
    {
        var first = Session("POS-1");
        var second = Session("POS-2");

        await _store.SaveCurrentAsync(first);
        await _store.SaveCurrentAsync(second);

        var open = await _store.GetOpenAsync();
        Assert.Equal(2, open.Count);
        Assert.Equal(second.SaleId, (await _store.GetCurrentAsync())!.SaleId);
        Assert.Equal(first.SaleId, (await _store.GetAsync(first.SaleId))!.SaleId);
    }

    [Fact]
    public async Task Save_UpdatesAnInvoiceWithoutStealingFocusFromTheActiveOne()
    {
        var background = Session("POS-1");
        var active = Session("POS-2");
        await _store.SaveCurrentAsync(background);
        await _store.SaveCurrentAsync(active);

        background.AmountPaid = 40m;
        await _store.SaveAsync(background);

        Assert.Equal(active.SaleId, (await _store.GetCurrentAsync())!.SaleId);
        Assert.Equal(40m, (await _store.GetAsync(background.SaleId))!.AmountPaid);
        Assert.Equal(2, (await _store.GetOpenAsync()).Count);
    }

    [Fact]
    public async Task SetActive_OnlySucceedsForAnOpenInvoice()
    {
        var invoice = Session("POS-1");
        await _store.SaveCurrentAsync(invoice);

        Assert.NotNull(await _store.SetActiveAsync(invoice.SaleId));
        Assert.Null(await _store.SetActiveAsync(Guid.NewGuid()));
        Assert.Equal(invoice.SaleId, (await _store.GetCurrentAsync())!.SaleId);
    }

    [Fact]
    public async Task Close_HandsTheScreenToTheMostRecentRemainingInvoice()
    {
        var first = Session("POS-1");
        var second = Session("POS-2");
        var third = Session("POS-3");
        await _store.SaveCurrentAsync(first);
        await _store.SaveCurrentAsync(second);
        await _store.SaveCurrentAsync(third);

        await _store.CloseAsync(third.SaleId);

        Assert.Equal(second.SaleId, (await _store.GetCurrentAsync())!.SaleId);
        Assert.Equal(2, (await _store.GetOpenAsync()).Count);
    }

    [Fact]
    public async Task Close_LeavesNothingActiveWhenTheLastInvoiceGoes()
    {
        var only = Session("POS-1");
        await _store.SaveCurrentAsync(only);

        await _store.CloseAsync(only.SaleId);

        Assert.Null(await _store.GetCurrentAsync());
        Assert.Empty(await _store.GetOpenAsync());
    }

    [Fact]
    public async Task Close_IgnoresAnInvoiceThatIsNotOpen()
    {
        var invoice = Session("POS-1");
        await _store.SaveCurrentAsync(invoice);

        await _store.CloseAsync(Guid.NewGuid());

        Assert.Equal(invoice.SaleId, (await _store.GetCurrentAsync())!.SaleId);
    }

    [Fact]
    public async Task Suspend_MovesAnInvoiceOutOfTheOpenSetAndResumeBringsItBack()
    {
        var parked = Session("POS-1");
        var stays = Session("POS-2");
        await _store.SaveCurrentAsync(parked);
        await _store.SaveCurrentAsync(stays);

        await _store.SuspendAsync(parked);

        Assert.Equal(stays.SaleId, Assert.Single(await _store.GetOpenAsync()).SaleId);
        Assert.True(Assert.Single(await _store.GetHeldAsync()).IsSuspended);

        var resumed = await _store.ResumeAsync(parked.SaleId);

        Assert.NotNull(resumed);
        Assert.False(resumed!.IsSuspended);
        Assert.Equal(2, (await _store.GetOpenAsync()).Count);
        Assert.Empty(await _store.GetHeldAsync());
        Assert.Equal(parked.SaleId, (await _store.GetCurrentAsync())!.SaleId);
    }

    [Fact]
    public async Task Resume_ReturnsNullForASaleThatWasNeverHeld()
    {
        Assert.Null(await _store.ResumeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetReservedQuantity_SumsTheOtherOpenInvoicesOnly()
    {
        var productId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var first = Session("POS-1", (productId, 3), (otherProductId, 7));
        var second = Session("POS-2", (productId, 4));
        var held = Session("POS-3", (productId, 9));
        await _store.SaveCurrentAsync(first);
        await _store.SaveCurrentAsync(second);
        await _store.SaveCurrentAsync(held);
        await _store.SuspendAsync(held);

        // Held sales are closed carts, so they do not hold stock away from the open ones.
        Assert.Equal(4, await _store.GetReservedQuantityAsync(productId, first.SaleId));
        Assert.Equal(3, await _store.GetReservedQuantityAsync(productId, second.SaleId));
        Assert.Equal(7, await _store.GetReservedQuantityAsync(productId, excludingSaleId: null));
        Assert.Equal(0, await _store.GetReservedQuantityAsync(Guid.NewGuid(), null));
    }

    [Fact]
    public async Task State_SurvivesARestartOfTheApplication()
    {
        var first = Session("POS-1", (Guid.NewGuid(), 2));
        var second = Session("POS-2");
        await _store.SaveCurrentAsync(first);
        await _store.SaveCurrentAsync(second);
        await _store.SuspendAsync(first);

        var reopened = new PosSaleSessionStore(NullLogger<PosSaleSessionStore>.Instance, _directory);

        Assert.Equal(second.SaleId, (await reopened.GetCurrentAsync())!.SaleId);
        Assert.Equal(first.SaleId, Assert.Single(await reopened.GetHeldAsync()).SaleId);
    }

    [Fact]
    public async Task LegacyCartFile_IsMigratedToTheOpenInvoiceSet()
    {
        // Files written before invoices could be open side by side hold a single CurrentSale.
        var legacySaleId = Guid.NewGuid();
        var legacy = $$"""
        {
          "CurrentSale": {
            "SaleId": "{{legacySaleId}}",
            "InvoiceNumber": "POS-LEGACY",
            "Items": [],
            "PaymentMethod": 0
          },
          "HeldSales": []
        }
        """;
        await File.WriteAllTextAsync(Path.Combine(_directory, "pos-sales.json"), legacy);

        var current = await _store.GetCurrentAsync();

        Assert.NotNull(current);
        Assert.Equal(legacySaleId, current!.SaleId);
        Assert.Equal("POS-LEGACY", current.InvoiceNumber);
        Assert.Equal(legacySaleId, Assert.Single(await _store.GetOpenAsync()).SaleId);
    }

    [Fact]
    public async Task UnreadableCartFile_IsQuarantinedRatherThanBrickingTheTill()
    {
        var path = Path.Combine(_directory, "pos-sales.json");
        await File.WriteAllTextAsync(path, "{ \"OpenSales\": [ { \"SaleId\": ");

        // A truncated file used to throw on every later POS operation, leaving the till unusable.
        Assert.Null(await _store.GetCurrentAsync());
        Assert.True(File.Exists(path + ".corrupt"));

        var invoice = Session("POS-1");
        await _store.SaveCurrentAsync(invoice);

        Assert.Equal(invoice.SaleId, (await _store.GetCurrentAsync())!.SaleId);
    }

    [Fact]
    public async Task SavedFileIsValidJsonWithNoStrayTemporaryFileLeftBehind()
    {
        await _store.SaveCurrentAsync(Session("POS-1", (Guid.NewGuid(), 1)));

        var path = Path.Combine(_directory, "pos-sales.json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));

        Assert.True(document.RootElement.TryGetProperty("OpenSales", out var openSales));
        Assert.Equal(1, openSales.GetArrayLength());
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public async Task SessionLock_StopsTwoCommandsFromSavingOverOneAnother()
    {
        var invoice = Session("POS-1");
        await _store.SaveCurrentAsync(invoice);

        // What every mutating handler does: take the lock, load the invoice, change it, write it back.
        async Task AddLineAsync(string barcode)
        {
            await using var sessionLock = await _store.LockAsync();
            var loaded = await _store.GetAsync(invoice.SaleId);
            await Task.Yield();
            loaded!.Items.Add(new SaleCartItemDto { ProductId = Guid.NewGuid(), Barcode = barcode, Quantity = 1, UnitPrice = 10m });
            await _store.SaveAsync(loaded);
        }

        await Task.WhenAll(AddLineAsync("A"), AddLineAsync("B"));

        // Without the lock one of the two scans was silently dropped, which undercharges the customer.
        var final = await _store.GetAsync(invoice.SaleId);
        Assert.Equal(2, final!.Items.Count);
        Assert.Contains(final.Items, item => item.Barcode == "A");
        Assert.Contains(final.Items, item => item.Barcode == "B");
    }

    [Fact]
    public async Task SessionLock_IsReleasedSoALaterCommandCanTakeIt()
    {
        await using (await _store.LockAsync())
        {
            // held
        }

        var second = await _store.LockAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await second.DisposeAsync();

        Assert.True(true);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover temp folder is not worth failing a test over.
        }

        GC.SuppressFinalize(this);
    }

    private static SaleSessionDto Session(string invoiceNumber, params (Guid ProductId, int Quantity)[] lines)
    {
        var sale = new SaleSessionDto
        {
            InvoiceNumber = invoiceNumber,
            CashierName = "Cashier",
            PaymentMethod = PaymentMethod.Cash
        };

        foreach (var line in lines)
        {
            sale.Items.Add(new SaleCartItemDto
            {
                ProductId = line.ProductId,
                Title = "Book",
                Barcode = "BK100",
                Quantity = line.Quantity,
                UnitPrice = 10m
            });
        }

        return sale;
    }
}
