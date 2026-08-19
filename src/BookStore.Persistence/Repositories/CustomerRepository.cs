using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ReadModels;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework customer repository.
/// </summary>
public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    private readonly BookStoreDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public CustomerRepository(BookStoreDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<bool> ExistsByPhoneAsync(string phone, Guid? excludedCustomerId = null, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePhone(phone);
        var query = _dbContext.Customers
            .FromSqlInterpolated($"SELECT * FROM Customers WHERE REPLACE(REPLACE(Phone, ' ', ''), '-', '') = {normalized}")
            .AsNoTracking();
        if (excludedCustomerId.HasValue)
        {
            query = query.Where(customer => customer.Id != excludedCustomerId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<CustomerListReadModel>> SearchAsync(string? searchTerm, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyCustomerFilter(searchTerm, isActive);
        return await ProjectSummary(query)
            .OrderBy(customer => customer.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        return ApplyCustomerFilter(searchTerm, isActive).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<CustomerListReadModel?> GetSummaryByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return ProjectSummary(_dbContext.Customers.AsNoTracking().Where(customer => customer.Id == customerId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<CustomerSaleHistoryReadModel>> GetSalesHistoryAsync(Guid customerId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await ApplySalesHistoryFilter(customerId, dateFrom, dateTo)
            .Select(sale => new CustomerSaleHistoryReadModel
            {
                SaleId = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                SaleDate = sale.SaleDate,
                Total = sale.Total,
                Discount = sale.Discount,
                Tax = sale.Tax,
                PaymentMethod = sale.PaymentMethod,
                SaleStatus = sale.Status
            })
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountSalesHistoryAsync(Guid customerId, DateTimeOffset? dateFrom = null, DateTimeOffset? dateTo = null, CancellationToken cancellationToken = default)
    {
        return ApplySalesHistoryFilter(customerId, dateFrom, dateTo).CountAsync(cancellationToken);
    }

    private IQueryable<Customer> ApplyCustomerFilter(string? searchTerm, bool? isActive)
    {
        var query = _dbContext.Customers.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(customer => customer.IsActive == isActive.Value);
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var pattern = $"%{searchTerm.Trim().ToUpperInvariant()}%";
        var phone = NormalizePhone(searchTerm);
        var phonePattern = $"%{phone}%";
        query = string.IsNullOrWhiteSpace(phone)
            ? _dbContext.Customers
                .FromSqlInterpolated($"SELECT * FROM Customers WHERE UPPER(FullName) LIKE {pattern} OR UPPER(Email) LIKE {pattern}")
                .AsNoTracking()
            : _dbContext.Customers
                .FromSqlInterpolated($"SELECT * FROM Customers WHERE UPPER(FullName) LIKE {pattern} OR UPPER(Email) LIKE {pattern} OR REPLACE(REPLACE(Phone, ' ', ''), '-', '') LIKE {phonePattern}")
                .AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(customer => customer.IsActive == isActive.Value);
        }

        return query;
    }

    private IQueryable<Sale> ApplySalesHistoryFilter(Guid customerId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo)
    {
        var query = _dbContext.Sales.AsNoTracking().Where(sale => sale.CustomerId == customerId);
        if (dateFrom.HasValue)
        {
            query = query.Where(sale => sale.SaleDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            // Exclusive upper bound: callers pass the start of the day after the one they mean.
            query = query.Where(sale => sale.SaleDate < dateTo.Value);
        }

        return query;
    }

    private IQueryable<CustomerListReadModel> ProjectSummary(IQueryable<Customer> query)
    {
        return query.Select(customer => new CustomerListReadModel
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Phone = customer.Phone == null ? string.Empty : customer.Phone.Value,
            Email = customer.Email == null ? null : customer.Email.Value,
            Address = customer.Address == null ? null : customer.Address.Line1,
            SalesCount = _dbContext.Sales.Count(sale => sale.CustomerId == customer.Id && sale.Status != Domain.Enums.SaleStatus.Cancelled),
            TotalPurchases = _dbContext.Sales.Where(sale => sale.CustomerId == customer.Id && sale.Status != Domain.Enums.SaleStatus.Cancelled).Sum(sale => (decimal?)sale.Total) ?? 0m,
            LastPurchaseDate = _dbContext.Sales.Where(sale => sale.CustomerId == customer.Id && sale.Status != Domain.Enums.SaleStatus.Cancelled).Max(sale => (DateTimeOffset?)sale.SaleDate),
            IsActive = customer.IsActive,
            IsDeleted = customer.IsDeleted,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        });
    }

    private static string NormalizePhone(string phone) => new(phone.Where(char.IsDigit).ToArray());
}
