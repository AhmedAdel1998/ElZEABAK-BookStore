using AutoMapper;
using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Features.Inventory.Commands.AdjustStock;
using BookStore.Application.Features.Inventory.Commands.DecreaseStock;
using BookStore.Application.Features.Inventory.Commands.IncreaseStock;
using BookStore.Application.Features.Inventory.Handlers;
using BookStore.Application.Features.Inventory.Mappings;
using BookStore.Application.Features.Inventory.Queries.GetInventoryHistory;
using BookStore.Application.Features.Inventory.Validators;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Domain.Specifications;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Constants;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Application.Tests;

public class InventoryModuleTests
{
    [Fact]
    public async Task IncreaseStock_CreatesLedgerEntry_AndUpdatesProductQuantity()
    {
        var fixture = new InventoryFixture();
        var product = fixture.Repository.AddProduct("BC-100", "Clean Code", quantity: 5);

        var result = await fixture.CreateIncreaseHandler().HandleAsync(new IncreaseStockRequest(product.Id, 3, "Count correction"));

        Assert.True(result.IsSuccess);
        Assert.Equal(8, product.Quantity);
        Assert.Single(fixture.InventoryRepository.Transactions);
        Assert.Equal(5, fixture.InventoryRepository.Transactions.Single().QuantityBefore);
        Assert.Equal(8, fixture.InventoryRepository.Transactions.Single().QuantityAfter);
    }

    [Fact]
    public async Task DecreaseStock_CreatesLedgerEntry_AndUpdatesProductQuantity()
    {
        var fixture = new InventoryFixture();
        var product = fixture.Repository.AddProduct("BC-100", "Clean Code", quantity: 5);

        var result = await fixture.CreateDecreaseHandler().HandleAsync(new DecreaseStockRequest(product.Id, 2, "Damage"));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, product.Quantity);
        Assert.Equal(-2, fixture.InventoryRepository.Transactions.Single().Quantity);
    }

    [Fact]
    public async Task AdjustStock_SetsTargetQuantity_AndRecordsDifference()
    {
        var fixture = new InventoryFixture();
        var product = fixture.Repository.AddProduct("BC-100", "Clean Code", quantity: 5);

        var result = await fixture.CreateAdjustHandler().HandleAsync(new AdjustStockRequest(product.Id, 9, InventoryTransactionType.Correction, "Shelf count"));

        Assert.True(result.IsSuccess);
        Assert.Equal(9, product.Quantity);
        Assert.Equal(4, fixture.InventoryRepository.Transactions.Single().Quantity);
    }

    [Fact]
    public async Task DecreaseStock_PreventsNegativeStock()
    {
        var fixture = new InventoryFixture();
        var product = fixture.Repository.AddProduct("BC-100", "Clean Code", quantity: 1);

        var result = await fixture.CreateDecreaseHandler().HandleAsync(new DecreaseStockRequest(product.Id, 2, "Damage"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Stock cannot become negative.", result.Error);
        Assert.Empty(fixture.InventoryRepository.Transactions);
    }

    [Fact]
    public async Task InventoryHistory_ReturnsLedgerEntries()
    {
        var fixture = new InventoryFixture();
        var product = fixture.Repository.AddProduct("BC-100", "Clean Code", quantity: 1);
        await fixture.CreateIncreaseHandler().HandleAsync(new IncreaseStockRequest(product.Id, 2, "Restock"));

        var result = await fixture.CreateHistoryHandler().HandleAsync(new GetInventoryHistoryRequest(product.Id));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(2, result.Value.Items.Single().QuantityChange);
    }

    [Fact]
    public void PermissionChecks_UseInventoryPermissionConstants()
    {
        var authorization = new FakeAuthorizationService([PermissionConstants.InventoryView, PermissionConstants.InventoryAdjust]);

        Assert.True(authorization.HasPermission(PermissionConstants.InventoryView));
        Assert.True(authorization.HasPermission(PermissionConstants.InventoryAdjust));
        Assert.False(authorization.HasPermission(PermissionConstants.InventoryExport));
    }

    [Fact]
    public async Task IncreaseStockValidator_RequiresReason()
    {
        var validator = new IncreaseStockRequestValidator();

        var result = await validator.ValidateAsync(new IncreaseStockRequest(Guid.NewGuid(), 1, string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Reason is required.");
    }

    private sealed class InventoryFixture
    {
        public InventoryFixture()
        {
            Repository = new FakeProductRepository();
            InventoryRepository = new FakeInventoryRepository();
            UnitOfWork = new FakeUnitOfWork(Repository, InventoryRepository);
            CurrentUser = new FakeCurrentUserService();
            Mapper = new MapperConfiguration(configuration => configuration.AddProfile<InventoryMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        }

        public FakeProductRepository Repository { get; }
        public FakeInventoryRepository InventoryRepository { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public FakeCurrentUserService CurrentUser { get; }
        public IMapper Mapper { get; }

        public IncreaseStockHandler CreateIncreaseHandler()
        {
            return new IncreaseStockHandler(new IncreaseStockRequestValidator(), CreateMovementService(), NullLogger<IncreaseStockHandler>.Instance);
        }

        public DecreaseStockHandler CreateDecreaseHandler()
        {
            return new DecreaseStockHandler(new DecreaseStockRequestValidator(), CreateMovementService(), NullLogger<DecreaseStockHandler>.Instance);
        }

        public AdjustStockHandler CreateAdjustHandler()
        {
            return new AdjustStockHandler(UnitOfWork, new AdjustStockRequestValidator(), CreateMovementService(), NullLogger<AdjustStockHandler>.Instance);
        }

        public GetInventoryHistoryHandler CreateHistoryHandler()
        {
            return new GetInventoryHistoryHandler(UnitOfWork, new GetInventoryHistoryRequestValidator(), Mapper, NullLogger<GetInventoryHistoryHandler>.Instance);
        }

        private InventoryMovementService CreateMovementService()
        {
            return new InventoryMovementService(UnitOfWork, CurrentUser, NullLogger<InventoryMovementService>.Instance);
        }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public List<Product> Products { get; } = [];

        public Product AddProduct(string barcode, string title, int quantity)
        {
            var product = new Product(new Barcode(barcode), title, 10, 15, Guid.NewGuid());
            product.UpdateDetails(null, null, "Author", null, null, null);
            product.SetQuantity(quantity);
            Products.Add(product);
            return product;
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Products.FirstOrDefault(product => product.Id == id));
        public Task<IReadOnlyCollection<Product>> ListAsync(ISpecification<Product>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyCollection<Product>)Products.ToArray());
        public Task AddAsync(Product product, CancellationToken cancellationToken = default) { Products.Add(product); return Task.CompletedTask; }
        public void Remove(Product product) => Products.Remove(product);
        public Task<IReadOnlyCollection<Product>> SearchAsync(string? searchTerm, bool? isActive, bool lowStockOnly, Guid? categoryId, decimal? minPrice, decimal? maxPrice, int? minQuantity, int? maxQuantity, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyCollection<Product>)Products.ToArray());
        public Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, bool lowStockOnly = false, Guid? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null, int? minQuantity = null, int? maxQuantity = null, CancellationToken cancellationToken = default) => Task.FromResult(Products.Count);
        public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludedProductId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ExistsByIsbnAsync(string isbn, Guid? excludedProductId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasCompletedSaleReferencesAsync(Guid productId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(Products.FirstOrDefault(product => product.Barcode.Value.Equals(barcode, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        public List<InventoryTransaction> Transactions { get; } = [];

        public Task<InventoryTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.FirstOrDefault(transaction => transaction.Id == id));
        public Task<IReadOnlyCollection<InventoryTransaction>> ListAsync(ISpecification<InventoryTransaction>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyCollection<InventoryTransaction>)Transactions.ToArray());
        public Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default) { Transactions.Add(transaction); return Task.CompletedTask; }
        public Task<IReadOnlyCollection<InventoryTransaction>> SearchHistoryAsync(Guid? productId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, InventoryTransactionType? transactionType, Guid? userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<InventoryTransaction> rows = Transactions.Where(transaction => productId is null || transaction.ProductId == productId.Value).ToArray();
            return Task.FromResult(rows);
        }

        public Task<int> CountHistoryAsync(Guid? productId = null, DateTimeOffset? dateFrom = null, DateTimeOffset? dateTo = null, InventoryTransactionType? transactionType = null, Guid? userId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Transactions.Count(transaction => productId is null || transaction.ProductId == productId.Value));
        }
    }

    private sealed class FakeUnitOfWork(FakeProductRepository products, FakeInventoryRepository inventory) : IUnitOfWork
    {
        public IProductRepository Products { get; } = products;
        public IInventoryRepository Inventory { get; } = inventory;
        public ICategoryRepository Categories => throw new NotSupportedException();
        public ICustomerRepository Customers => throw new NotSupportedException();
        public ISupplierRepository Suppliers => throw new NotSupportedException();
        public IUserRepository Users => throw new NotSupportedException();
        public IRoleRepository Roles => throw new NotSupportedException();
        public ISaleRepository Sales => throw new NotSupportedException();
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public string? Username => "tester";
        public string? FullName => "Test User";
        public string? Role => "Admin";
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
}
