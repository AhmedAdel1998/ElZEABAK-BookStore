using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ReadModels;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework supplier repository.
/// </summary>
public class SupplierRepository : Repository<Supplier>, ISupplierRepository
{
    private readonly BookStoreDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SupplierRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public SupplierRepository(BookStoreDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<bool> ExistsByCompanyNameAsync(string companyName, Guid? excludedSupplierId = null, CancellationToken cancellationToken = default)
    {
        var normalized = companyName.Trim().ToUpperInvariant();
        var query = _dbContext.Suppliers.AsNoTracking().Where(supplier => supplier.CompanyName.ToUpper() == normalized);
        if (excludedSupplierId.HasValue)
        {
            query = query.Where(supplier => supplier.Id != excludedSupplierId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SupplierListReadModel>> SearchAsync(string? searchTerm, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplySupplierFilter(searchTerm, isActive);
        return await ProjectSummary(query)
            .OrderBy(supplier => supplier.CompanyName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        return ApplySupplierFilter(searchTerm, isActive).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<SupplierListReadModel?> GetSummaryByIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return ProjectSummary(_dbContext.Suppliers.AsNoTracking().Where(supplier => supplier.Id == supplierId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SupplierProductReadModel>> GetProductsAsync(Guid supplierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductSuppliers
            .AsNoTracking()
            .Where(productSupplier => productSupplier.SupplierId == supplierId)
            .OrderBy(productSupplier => productSupplier.Product!.Title)
            .Select(productSupplier => new SupplierProductReadModel
            {
                ProductId = productSupplier.ProductId,
                Barcode = productSupplier.Product!.Barcode.Value,
                ISBN = productSupplier.Product.ISBN == null ? null : productSupplier.Product.ISBN.Value,
                Title = productSupplier.Product.Title,
                Author = productSupplier.Product.Author,
                Category = productSupplier.Product.Category == null ? string.Empty : productSupplier.Product.Category.Name,
                PurchasePrice = productSupplier.Product.PurchasePrice,
                SellingPrice = productSupplier.Product.SellingPrice,
                CurrentStock = productSupplier.Product.Quantity,
                IsActive = productSupplier.Product.IsActive
            })
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountProductsAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductSuppliers.AsNoTracking().CountAsync(productSupplier => productSupplier.SupplierId == supplierId, cancellationToken);
    }

    private IQueryable<Supplier> ApplySupplierFilter(string? searchTerm, bool? isActive)
    {
        var query = _dbContext.Suppliers.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(supplier => supplier.IsActive == isActive.Value);
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var pattern = $"%{searchTerm.Trim().ToUpperInvariant()}%";
        var phone = NormalizePhone(searchTerm);
        var phonePattern = $"%{phone}%";
        query = string.IsNullOrWhiteSpace(phone)
            ? _dbContext.Suppliers
                .FromSqlInterpolated($"SELECT * FROM Suppliers WHERE UPPER(CompanyName) LIKE {pattern} OR UPPER(ContactName) LIKE {pattern} OR UPPER(Email) LIKE {pattern}")
                .AsNoTracking()
            : _dbContext.Suppliers
                .FromSqlInterpolated($"SELECT * FROM Suppliers WHERE UPPER(CompanyName) LIKE {pattern} OR UPPER(ContactName) LIKE {pattern} OR UPPER(Email) LIKE {pattern} OR REPLACE(REPLACE(Phone, ' ', ''), '-', '') LIKE {phonePattern}")
                .AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(supplier => supplier.IsActive == isActive.Value);
        }

        return query;
    }

    private IQueryable<SupplierListReadModel> ProjectSummary(IQueryable<Supplier> query)
    {
        return query.Select(supplier => new SupplierListReadModel
        {
            Id = supplier.Id,
            CompanyName = supplier.CompanyName,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone == null ? string.Empty : supplier.Phone.Value,
            Email = supplier.Email == null ? null : supplier.Email.Value,
            Address = supplier.Address == null ? null : supplier.Address.Line1,
            Notes = supplier.Notes,
            ProductCount = _dbContext.ProductSuppliers.Count(productSupplier => productSupplier.SupplierId == supplier.Id),
            IsActive = supplier.IsActive,
            IsDeleted = supplier.IsDeleted,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        });
    }

    private static string NormalizePhone(string value) => new(value.Where(char.IsDigit).ToArray());
}
