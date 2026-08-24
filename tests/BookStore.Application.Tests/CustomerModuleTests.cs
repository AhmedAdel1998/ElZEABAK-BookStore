using AutoMapper;
using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Features.Customers.Commands.CreateCustomer;
using BookStore.Application.Features.Customers.Commands.DeactivateCustomer;
using BookStore.Application.Features.Customers.Commands.DeleteCustomer;
using BookStore.Application.Features.Customers.Commands.UpdateCustomer;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Customers.Mappings;
using BookStore.Application.Features.Customers.Queries.GetCustomerSalesHistory;
using BookStore.Application.Features.Customers.Queries.SearchCustomers;
using BookStore.Application.Features.Customers.Validators;
using BookStore.Application.Features.Sales.Commands.ClearCustomer;
using BookStore.Application.Features.Sales.Commands.SelectCustomer;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Sales.Handlers;
using BookStore.Application.Features.Sales.Validators;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ReadModels;
using BookStore.Domain.Specifications;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Constants;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Application.Tests;

public class CustomerModuleTests
{
    [Fact]
    public async Task CreateCustomer_CreatesCustomer()
    {
        var fixture = new Fixture();

        var result = await fixture.Create.HandleAsync(new CreateCustomerRequest(new CustomerEditorModel { FullName = "Ahmed Adel", Phone = "+201001001000" }));

        Assert.True(result.IsSuccess);
        Assert.Single(fixture.CustomerRepository.Customers);
    }

    [Fact]
    public async Task UpdateCustomer_UpdatesProfile()
    {
        var fixture = new Fixture();
        var customer = fixture.AddCustomer();

        var result = await fixture.Update.HandleAsync(new UpdateCustomerRequest(new CustomerEditorModel { Id = customer.Id, FullName = "New Name", Phone = "+201001001001", Email = "a@b.com", Address = "Street", IsActive = false }));

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", customer.FullName);
        Assert.False(customer.IsActive);
    }

    [Fact]
    public async Task CreateCustomer_RejectsDuplicatePhone()
    {
        var fixture = new Fixture();
        fixture.AddCustomer(phone: "+201001001000");

        var result = await fixture.Create.HandleAsync(new CreateCustomerRequest(new CustomerEditorModel { FullName = "Duplicate", Phone = "+20 100-100-1000" }));

        Assert.False(result.IsSuccess);
        Assert.Contains("same phone", result.Error);
    }

    [Theory]
    [InlineData("bad-email", "+201001001000")]
    [InlineData("a@b.com", "123")]
    public async Task CreateCustomer_RejectsInvalidContact(string email, string phone)
    {
        var fixture = new Fixture();

        var result = await fixture.Create.HandleAsync(new CreateCustomerRequest(new CustomerEditorModel { FullName = "Ahmed", Phone = phone, Email = email }));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteCustomer_SoftDeletesAndDeactivates()
    {
        var fixture = new Fixture();
        var customer = fixture.AddCustomer();

        var result = await fixture.Delete.HandleAsync(new DeleteCustomerRequest(customer.Id));

        Assert.True(result.Succeeded);
        Assert.True(customer.IsDeleted);
        Assert.False(customer.IsActive);
    }

    [Fact]
    public async Task DeactivateCustomer_DeactivatesCustomer()
    {
        var fixture = new Fixture();
        var customer = fixture.AddCustomer();

        var result = await fixture.Deactivate.HandleAsync(new DeactivateCustomerRequest(customer.Id));

        Assert.True(result.Succeeded);
        Assert.False(customer.IsActive);
    }

    [Fact]
    public async Task SearchCustomers_SearchesByPhone()
    {
        var fixture = new Fixture();
        fixture.AddCustomer(phone: "+201001001000");

        var result = await fixture.Search.HandleAsync(new SearchCustomersRequest(new CustomerFilter { SearchTerm = "1001001000" }));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task CustomerSalesHistory_ReturnsProjection()
    {
        var fixture = new Fixture();
        var customer = fixture.AddCustomer();
        fixture.CustomerRepository.History.Add(new CustomerSaleHistoryReadModel { SaleId = Guid.NewGuid(), InvoiceNumber = "INV-1", SaleDate = DateTimeOffset.UtcNow, Total = 100, PaymentMethod = PaymentMethod.Cash, SaleStatus = SaleStatus.Completed });

        var result = await fixture.History.HandleAsync(new GetCustomerSalesHistoryRequest(customer.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("INV-1", result.Value!.Items.Single().InvoiceNumber);
    }

    [Fact]
    public async Task SelectCustomerForSale_AssignsCurrentSaleCustomer()
    {
        var fixture = new Fixture();
        var customer = fixture.AddCustomer();
        await fixture.SessionStore.SaveCurrentAsync(new SaleSessionDto { InvoiceNumber = "POS-1" });

        var result = await fixture.SelectCustomer.HandleAsync(new SelectCustomerForSaleRequest(customer.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value!.CustomerId);
    }

    [Fact]
    public async Task ClearCustomerFromSale_RestoresWalkIn()
    {
        var fixture = new Fixture();
        await fixture.SessionStore.SaveCurrentAsync(new SaleSessionDto { CustomerId = Guid.NewGuid(), CustomerName = "Ahmed" });

        var result = await fixture.ClearCustomer.HandleAsync(new ClearCustomerFromSaleRequest());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.CustomerId);
        Assert.Equal("Walk-in Customer", result.Value.CustomerName);
    }

    [Fact]
    public void Sale_AssignCustomer_SetsCustomerId()
    {
        var customerId = Guid.NewGuid();
        var sale = new Sale("INV-1", Guid.NewGuid(), PaymentMethod.Cash);

        sale.AssignCustomer(customerId);

        Assert.Equal(customerId, sale.CustomerId);
    }

    [Fact]
    public void PermissionConstants_ExposeCustomerPermissions()
    {
        var authorization = new FakeAuthorizationService([PermissionConstants.CustomerView, PermissionConstants.CustomerCreate]);

        Assert.True(authorization.HasPermission(PermissionConstants.CustomerView));
        Assert.True(authorization.HasPermission(PermissionConstants.CustomerCreate));
        Assert.False(authorization.HasPermission(PermissionConstants.CustomerDelete));
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            var mapper = new MapperConfiguration(configuration => configuration.AddProfile<CustomerMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
            UnitOfWork = new FakeUnitOfWork(CustomerRepository);
            IValidator<CustomerEditorModel> customerValidator = new CustomerEditorModelValidator(CustomerRepository);
            Create = new CreateCustomerHandler(UnitOfWork, new CreateCustomerRequestValidator(customerValidator), mapper, NullLogger<CreateCustomerHandler>.Instance);
            Update = new UpdateCustomerHandler(UnitOfWork, new UpdateCustomerRequestValidator(customerValidator), mapper, NullLogger<UpdateCustomerHandler>.Instance);
            Delete = new DeleteCustomerHandler(UnitOfWork, new DeleteCustomerRequestValidator(), NullLogger<DeleteCustomerHandler>.Instance);
            Deactivate = new DeactivateCustomerHandler(UnitOfWork, new DeactivateCustomerRequestValidator(), NullLogger<DeactivateCustomerHandler>.Instance);
            Search = new SearchCustomersHandler(UnitOfWork, new SearchCustomersRequestValidator(), mapper, NullLogger<SearchCustomersHandler>.Instance);
            History = new GetCustomerSalesHistoryHandler(UnitOfWork, new GetCustomerSalesHistoryRequestValidator(), mapper);
            SelectCustomer = new SelectCustomerForSaleHandler(UnitOfWork, SessionStore, new SelectCustomerForSaleRequestValidator(), NullLogger<SelectCustomerForSaleHandler>.Instance);
            ClearCustomer = new ClearCustomerFromSaleHandler(SessionStore);
        }

        public FakeCustomerRepository CustomerRepository { get; } = new();
        public InMemoryPosSaleSessionStore SessionStore { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; }
        public CreateCustomerHandler Create { get; }
        public UpdateCustomerHandler Update { get; }
        public DeleteCustomerHandler Delete { get; }
        public DeactivateCustomerHandler Deactivate { get; }
        public SearchCustomersHandler Search { get; }
        public GetCustomerSalesHistoryHandler History { get; }
        public SelectCustomerForSaleHandler SelectCustomer { get; }
        public ClearCustomerFromSaleHandler ClearCustomer { get; }

        public Customer AddCustomer(string name = "Ahmed Adel", string phone = "+201001001000")
        {
            var customer = new Customer(name);
            customer.UpdateContact(new PhoneNumber(phone), null, null);
            CustomerRepository.Customers.Add(customer);
            return customer;
        }
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = [];
        public List<CustomerSaleHistoryReadModel> History { get; } = [];

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == id && !customer.IsDeleted));
        public Task<IReadOnlyCollection<Customer>> ListAsync(ISpecification<Customer>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Customer>>(Customers);
        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) { Customers.Add(customer); return Task.CompletedTask; }
        public Task<bool> ExistsByPhoneAsync(string phone, Guid? excludedCustomerId = null, CancellationToken cancellationToken = default)
        {
            var normalized = Normalize(phone);
            return Task.FromResult(Customers.Any(customer => customer.Phone is not null && Normalize(customer.Phone.Value) == normalized && customer.Id != excludedCustomerId));
        }

        public Task<IReadOnlyCollection<CustomerListReadModel>> SearchAsync(string? searchTerm, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var normalized = Normalize(searchTerm ?? string.Empty);
            var rows = Customers
                .Where(customer => !customer.IsDeleted)
                .Where(customer => isActive is null || customer.IsActive == isActive)
                .Where(customer => string.IsNullOrWhiteSpace(searchTerm) || customer.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || (customer.Phone is not null && Normalize(customer.Phone.Value).Contains(normalized, StringComparison.OrdinalIgnoreCase)) || (customer.Email?.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(ToReadModel)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<CustomerListReadModel>>(rows);
        }

        public Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default) => Task.FromResult(Customers.Count(customer => !customer.IsDeleted && (isActive is null || customer.IsActive == isActive)));
        public Task<CustomerListReadModel?> GetSummaryByIdAsync(Guid customerId, CancellationToken cancellationToken = default) => Task.FromResult(Customers.Where(customer => customer.Id == customerId && !customer.IsDeleted).Select(ToReadModel).FirstOrDefault());
        public Task<IReadOnlyCollection<CustomerSaleHistoryReadModel>> GetSalesHistoryAsync(Guid customerId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CustomerSaleHistoryReadModel>>(History.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray());
        public Task<int> CountSalesHistoryAsync(Guid customerId, DateTimeOffset? dateFrom = null, DateTimeOffset? dateTo = null, CancellationToken cancellationToken = default) => Task.FromResult(History.Count);

        private static CustomerListReadModel ToReadModel(Customer customer) => new()
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Phone = customer.Phone?.Value ?? string.Empty,
            Email = customer.Email?.Value,
            Address = customer.Address?.Line1,
            IsActive = customer.IsActive,
            IsDeleted = customer.IsDeleted,
            CreatedAt = customer.CreatedAt
        };

        private static string Normalize(string value) => new(value.Where(char.IsDigit).ToArray());
    }

    private sealed class FakeUnitOfWork(ICustomerRepository customers) : IUnitOfWork
    {
        public IProductRepository Products => null!;
        public ICategoryRepository Categories => null!;
        public ICustomerRepository Customers => customers;
        public ISupplierRepository Suppliers => null!;
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
