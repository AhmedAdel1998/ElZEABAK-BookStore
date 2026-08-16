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
        Assert.Equal(28, result.Value.Summary.Tax);
        Assert.Equal(208, result.Value.Summary.GrandTotal);
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
        Assert.Equal("Sale completed, but receipt printing failed.", result.Value.ReceiptPrintError);
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

    private sealed class Fixture
    {
        public Fixture(IReadOnlyCollection<string>? permissions = null)
        {
            Authorization = new FakeAuthorizationService(permissions ?? [PermissionConstants.SalesCancel, PermissionConstants.SalesSuspend, PermissionConstants.SalesComplete, PermissionConstants.SalesApplyDiscount]);
            UnitOfWork = new FakeUnitOfWork(ProductRepository, InventoryRepository, SaleRepository);
            Pricing = new PricingService(new FakeSettingsService(Settings.Value));
            StartSale = new StartSaleHandler(CurrentUser, Store, Pricing, new StartSaleRequestValidator(), NullLogger<StartSaleHandler>.Instance);
            AddItem = new AddItemHandler(UnitOfWork, Store, Pricing, new AddItemRequestValidator());
            UpdateQuantity = new UpdateItemQuantityHandler(Store, Pricing, new UpdateItemQuantityRequestValidator());
            RemoveItem = new RemoveItemHandler(Store, Pricing, new RemoveItemRequestValidator());
            ApplyLineDiscount = new ApplyLineDiscountHandler(Authorization, Store, Pricing, new ApplyLineDiscountRequestValidator());
            ApplyInvoiceDiscount = new ApplyInvoiceDiscountHandler(Authorization, Store, Pricing, new ApplyInvoiceDiscountRequestValidator());
            SuspendSale = new SuspendSaleHandler(Authorization, Store);
            ResumeSale = new ResumeSaleHandler(Store, new ResumeSaleRequestValidator());
            CheckoutGuard = new FakeCheckoutConcurrencyGuard();
            CompleteSale = new CompleteSaleHandler(Authorization, CurrentUser, UnitOfWork, Store, Pricing, ReceiptService, CheckoutGuard, new CompleteSaleRequestValidator(), NullLogger<CompleteSaleHandler>.Instance);
        }

        public OptionsWrapper<ApplicationSettings> Settings { get; } = new(new ApplicationSettings());
        public FakeCurrentUserService CurrentUser { get; } = new();
        public FakeAuthorizationService Authorization { get; }
        public FakePosSaleSessionStore Store { get; } = new();
        public FakeProductRepository ProductRepository { get; } = new();
        public FakeInventoryRepository InventoryRepository { get; } = new();
        public FakeSaleRepository SaleRepository { get; } = new();
        public FakeReceiptService ReceiptService { get; } = new();
        public FakeBarcodeService BarcodeService { get; } = new();
        public FakeCheckoutConcurrencyGuard CheckoutGuard { get; }
        public PricingService Pricing { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public StartSaleHandler StartSale { get; }
        public AddItemHandler AddItem { get; }
        public UpdateItemQuantityHandler UpdateQuantity { get; }
        public RemoveItemHandler RemoveItem { get; }
        public ApplyLineDiscountHandler ApplyLineDiscount { get; }
        public ApplyInvoiceDiscountHandler ApplyInvoiceDiscount { get; }
        public SuspendSaleHandler SuspendSale { get; }
        public ResumeSaleHandler ResumeSale { get; }
        public CompleteSaleHandler CompleteSale { get; }

        public Product AddProduct(string barcode = "BK100", int quantity = 5, decimal price = 50)
        {
            var product = new Product(new Barcode(barcode), "Clean Architecture", 20, price, Guid.NewGuid());
            product.SetQuantity(quantity);
            ProductRepository.Products.Add(product);
            return product;
        }
    }

    private sealed class FakePosSaleSessionStore : IPosSaleSessionStore
    {
        private SaleSessionDto? _current;
        private readonly List<SaleSessionDto> _held = [];

        public Task<SaleSessionDto?> GetCurrentAsync(CancellationToken cancellationToken = default) => Task.FromResult(_current);
        public Task SaveCurrentAsync(SaleSessionDto sale, CancellationToken cancellationToken = default) { _current = sale; return Task.CompletedTask; }
        public Task ClearCurrentAsync(CancellationToken cancellationToken = default) { _current = null; return Task.CompletedTask; }
        public Task SuspendAsync(SaleSessionDto sale, CancellationToken cancellationToken = default) { _held.Add(sale); return Task.CompletedTask; }
        public Task<IReadOnlyCollection<SaleSessionDto>> GetHeldAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SaleSessionDto>>(_held.ToArray());
        public Task<SaleSessionDto?> ResumeAsync(Guid saleId, CancellationToken cancellationToken = default)
        {
            var sale = _held.FirstOrDefault(item => item.SaleId == saleId);
            if (sale is not null)
            {
                _held.Remove(sale);
                _current = sale;
            }

            return Task.FromResult(sale);
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
