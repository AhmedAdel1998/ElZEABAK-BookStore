using AutoMapper;
using BookStore.Application.Features.Suppliers.Commands.CreateSupplier;
using BookStore.Application.Features.Suppliers.Commands.DeactivateSupplier;
using BookStore.Application.Features.Suppliers.Commands.DeleteSupplier;
using BookStore.Application.Features.Suppliers.Commands.UpdateSupplier;
using BookStore.Application.Features.Suppliers.DTOs;
using BookStore.Application.Features.Suppliers.Handlers;
using BookStore.Application.Features.Suppliers.Mappings;
using BookStore.Application.Features.Suppliers.Queries.GetSupplierProducts;
using BookStore.Application.Features.Suppliers.Queries.SearchSuppliers;
using BookStore.Application.Features.Suppliers.Validators;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ReadModels;
using BookStore.Domain.Specifications;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Constants;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Application.Tests;

public class SupplierModuleTests
{
    [Fact]
    public async Task CreateSupplier_CreatesSupplier()
    {
        var fixture = new Fixture();

        var result = await fixture.Create.HandleAsync(new CreateSupplierRequest(new SupplierEditorModel { CompanyName = "ABC Distribution", Phone = "+201001001000" }));

        Assert.True(result.IsSuccess);
        Assert.Single(fixture.SupplierRepository.Suppliers);
    }

    [Fact]
    public async Task UpdateSupplier_UpdatesProfile()
    {
        var fixture = new Fixture();
        var supplier = fixture.AddSupplier();

        var result = await fixture.Update.HandleAsync(new UpdateSupplierRequest(new SupplierEditorModel { Id = supplier.Id, CompanyName = "Updated Supplier", ContactName = "Ahmed", Phone = "+201001001001", Email = "supplier@example.com", Address = "Street", Notes = "Preferred", IsActive = false }));

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Supplier", supplier.CompanyName);
        Assert.False(supplier.IsActive);
    }

    [Fact]
    public async Task CreateSupplier_RejectsDuplicateCompanyName()
    {
        var fixture = new Fixture();
        fixture.AddSupplier(companyName: "ABC Distribution");

        var result = await fixture.Create.HandleAsync(new CreateSupplierRequest(new SupplierEditorModel { CompanyName = "abc distribution", Phone = "+201001001000" }));

        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error);
    }

    [Theory]
    [InlineData("bad-email", "+201001001000")]
    [InlineData("supplier@example.com", "123")]
    [InlineData("supplier@example.com", "")]
    public async Task CreateSupplier_RejectsInvalidContact(string email, string phone)
    {
        var fixture = new Fixture();

        var result = await fixture.Create.HandleAsync(new CreateSupplierRequest(new SupplierEditorModel { CompanyName = "ABC Distribution", Phone = phone, Email = email }));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteSupplier_SoftDeletesAndDeactivates()
    {
        var fixture = new Fixture();
        var supplier = fixture.AddSupplier();

        var result = await fixture.Delete.HandleAsync(new DeleteSupplierRequest(supplier.Id));

        Assert.True(result.Succeeded);
        Assert.True(supplier.IsDeleted);
        Assert.False(supplier.IsActive);
    }

    [Fact]
    public async Task DeactivateSupplier_DeactivatesSupplier()
    {
        var fixture = new Fixture();
        var supplier = fixture.AddSupplier();

        var result = await fixture.Deactivate.HandleAsync(new DeactivateSupplierRequest(supplier.Id));

        Assert.True(result.Succeeded);
        Assert.False(supplier.IsActive);
    }

    [Fact]
    public async Task SearchSuppliers_SearchesByCompanyAndPhone()
    {
        var fixture = new Fixture();
        fixture.AddSupplier(companyName: "ABC Distribution", phone: "+201001001000");

        var companyResult = await fixture.Search.HandleAsync(new SearchSuppliersRequest(new SupplierFilter { SearchTerm = "abc" }));
        var phoneResult = await fixture.Search.HandleAsync(new SearchSuppliersRequest(new SupplierFilter { SearchTerm = "1001001000" }));

        Assert.Single(companyResult.Value!.Items);
        Assert.Single(phoneResult.Value!.Items);
    }

    [Fact]
    public async Task SupplierProducts_ReturnsProjection()
    {
        var fixture = new Fixture();
        var supplier = fixture.AddSupplier();
        fixture.SupplierRepository.Products.Add(new SupplierProductReadModel { ProductId = Guid.NewGuid(), Barcode = "BK100", Title = "Book", Category = "Books", PurchasePrice = 50, SellingPrice = 100, CurrentStock = 7, IsActive = true });

        var result = await fixture.Products.HandleAsync(new GetSupplierProductsRequest(supplier.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("Book", result.Value!.Items.Single().Title);
    }

    [Fact]
    public void PermissionConstants_ExposeSupplierPermissions()
    {
        var authorization = new FakeAuthorizationService([PermissionConstants.SupplierView, PermissionConstants.SupplierCreate, PermissionConstants.SupplierViewProducts]);

        Assert.True(authorization.HasPermission(PermissionConstants.SupplierView));
        Assert.True(authorization.HasPermission(PermissionConstants.SupplierCreate));
        Assert.False(authorization.HasPermission(PermissionConstants.SupplierDelete));
    }

    [Fact]
    public async Task SupplierValidator_RejectsMissingCompanyName()
    {
        var fixture = new Fixture();

        var result = await fixture.SupplierValidator.ValidateAsync(new SupplierEditorModel { Phone = "+201001001000" });

        Assert.False(result.IsValid);
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            var mapper = new MapperConfiguration(configuration => configuration.AddProfile<SupplierMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
            UnitOfWork = new FakeUnitOfWork(SupplierRepository);
            SupplierValidator = new SupplierEditorModelValidator(SupplierRepository);
            Create = new CreateSupplierHandler(UnitOfWork, new CreateSupplierRequestValidator(SupplierValidator), mapper, NullLogger<CreateSupplierHandler>.Instance);
            Update = new UpdateSupplierHandler(UnitOfWork, new UpdateSupplierRequestValidator(SupplierValidator), mapper, NullLogger<UpdateSupplierHandler>.Instance);
            Delete = new DeleteSupplierHandler(UnitOfWork, new DeleteSupplierRequestValidator(), NullLogger<DeleteSupplierHandler>.Instance);
            Deactivate = new DeactivateSupplierHandler(UnitOfWork, new DeactivateSupplierRequestValidator(), NullLogger<DeactivateSupplierHandler>.Instance);
            Search = new SearchSuppliersHandler(UnitOfWork, new SearchSuppliersRequestValidator(), mapper, NullLogger<SearchSuppliersHandler>.Instance);
            Products = new GetSupplierProductsHandler(UnitOfWork, new GetSupplierProductsRequestValidator(), mapper);
        }

        public FakeSupplierRepository SupplierRepository { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; }
        public IValidator<SupplierEditorModel> SupplierValidator { get; }
        public CreateSupplierHandler Create { get; }
        public UpdateSupplierHandler Update { get; }
        public DeleteSupplierHandler Delete { get; }
        public DeactivateSupplierHandler Deactivate { get; }
        public SearchSuppliersHandler Search { get; }
        public GetSupplierProductsHandler Products { get; }

        public Supplier AddSupplier(string companyName = "ABC Distribution", string phone = "+201001001000")
        {
            var supplier = new Supplier(companyName);
            supplier.UpdateDetails(null, new PhoneNumber(phone), null, null, null);
            SupplierRepository.Suppliers.Add(supplier);
            return supplier;
        }
    }

    private sealed class FakeSupplierRepository : ISupplierRepository
    {
        public List<Supplier> Suppliers { get; } = [];
        public List<SupplierProductReadModel> Products { get; } = [];

        public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Suppliers.FirstOrDefault(supplier => supplier.Id == id && !supplier.IsDeleted));
        public Task<IReadOnlyCollection<Supplier>> ListAsync(ISpecification<Supplier>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Supplier>>(Suppliers);
        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default) { Suppliers.Add(supplier); return Task.CompletedTask; }
        public Task<bool> ExistsByCompanyNameAsync(string companyName, Guid? excludedSupplierId = null, CancellationToken cancellationToken = default) => Task.FromResult(Suppliers.Any(supplier => string.Equals(supplier.CompanyName, companyName.Trim(), StringComparison.OrdinalIgnoreCase) && supplier.Id != excludedSupplierId));

        public Task<IReadOnlyCollection<SupplierListReadModel>> SearchAsync(string? searchTerm, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var normalized = Normalize(searchTerm ?? string.Empty);
            var rows = Suppliers
                .Where(supplier => !supplier.IsDeleted)
                .Where(supplier => isActive is null || supplier.IsActive == isActive)
                .Where(supplier => string.IsNullOrWhiteSpace(searchTerm) || supplier.CompanyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || (supplier.ContactName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) || (supplier.Phone is not null && Normalize(supplier.Phone.Value).Contains(normalized, StringComparison.OrdinalIgnoreCase)) || (supplier.Email?.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(ToReadModel)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<SupplierListReadModel>>(rows);
        }

        public Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default) => Task.FromResult(Suppliers.Count(supplier => !supplier.IsDeleted && (isActive is null || supplier.IsActive == isActive)));
        public Task<SupplierListReadModel?> GetSummaryByIdAsync(Guid supplierId, CancellationToken cancellationToken = default) => Task.FromResult(Suppliers.Where(supplier => supplier.Id == supplierId && !supplier.IsDeleted).Select(ToReadModel).FirstOrDefault());
        public Task<IReadOnlyCollection<SupplierProductReadModel>> GetProductsAsync(Guid supplierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SupplierProductReadModel>>(Products.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray());
        public Task<int> CountProductsAsync(Guid supplierId, CancellationToken cancellationToken = default) => Task.FromResult(Products.Count);

        private static SupplierListReadModel ToReadModel(Supplier supplier) => new()
        {
            Id = supplier.Id,
            CompanyName = supplier.CompanyName,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone?.Value ?? string.Empty,
            Email = supplier.Email?.Value,
            Address = supplier.Address?.Line1,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            IsDeleted = supplier.IsDeleted,
            CreatedAt = supplier.CreatedAt
        };

        private static string Normalize(string value) => new(value.Where(char.IsDigit).ToArray());
    }

    private sealed class FakeUnitOfWork(ISupplierRepository suppliers) : IUnitOfWork
    {
        public IProductRepository Products => null!;
        public ICategoryRepository Categories => null!;
        public ICustomerRepository Customers => null!;
        public ISupplierRepository Suppliers => suppliers;
        public IUserRepository Users => null!;
        public IRoleRepository Roles => null!;
        public ISaleRepository Sales => null!;
        public IInventoryRepository Inventory => null!;
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeAuthorizationService(IReadOnlyCollection<string> permissions) : IAuthorizationService
    {
        public bool HasPermission(string permission) => permissions.Contains(permission);
        public bool HasPermissions(params string[] permissionsToCheck) => permissionsToCheck.All(HasPermission);
        public bool HasRole(string role) => false;
        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
    }
}
