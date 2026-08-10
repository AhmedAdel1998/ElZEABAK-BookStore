using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Domain.Specifications;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework product repository.
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    private static readonly Func<BookStoreDbContext, string, IAsyncEnumerable<Product>> ProductsByTitleQuery =
        EF.CompileAsyncQuery((BookStoreDbContext context, string title) =>
            context.Products.AsNoTracking().Where(product => product.Title.Contains(title)));

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public ProductRepository(BookStoreDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Product>> ListAsync(ISpecification<Product>? specification = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = DbContext.Products
            .AsNoTracking()
            .Include(product => product.Category);

        if (specification?.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Products
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    /// <summary>
    /// Searches products by title using a compiled query.
    /// </summary>
    /// <param name="title">The title search text.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching products.</returns>
    public async Task<IReadOnlyCollection<Product>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();
        await foreach (var product in ProductsByTitleQuery(DbContext, title).WithCancellation(cancellationToken))
        {
            products.Add(product);
        }

        return products;
    }

    /// <inheritdoc />
    public void Remove(Product product)
    {
        RemoveEntity(product);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Product>> SearchAsync(
        string? searchTerm,
        bool? isActive,
        bool lowStockOnly,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? minQuantity,
        int? maxQuantity,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(DbContext.Products.AsNoTracking().Include(product => product.Category), searchTerm, isActive, lowStockOnly, categoryId, minPrice, maxPrice, minQuantity, maxQuantity)
            .OrderBy(product => product.Title)
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Max(pageSize, 1))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(
        string? searchTerm = null,
        bool? isActive = null,
        bool lowStockOnly = false,
        Guid? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? minQuantity = null,
        int? maxQuantity = null,
        CancellationToken cancellationToken = default)
    {
        return ApplyFilters(DbContext.Products.AsNoTracking(), searchTerm, isActive, lowStockOnly, categoryId, minPrice, maxPrice, minQuantity, maxQuantity)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludedProductId = null, CancellationToken cancellationToken = default)
    {
        var normalized = barcode.Trim().ToUpperInvariant();
        return DbContext.Products.AsNoTracking()
            .AnyAsync(product => product.Barcode.Value.ToUpper() == normalized && (excludedProductId == null || product.Id != excludedProductId.Value), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByIsbnAsync(string isbn, Guid? excludedProductId = null, CancellationToken cancellationToken = default)
    {
        var normalized = isbn.Replace("-", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
        return DbContext.Products.AsNoTracking()
            .AnyAsync(product => product.ISBN != null && product.ISBN.Value.ToUpper() == normalized && (excludedProductId == null || product.Id != excludedProductId.Value), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> HasCompletedSaleReferencesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return DbContext.SaleItems.AsNoTracking()
            .Where(item => item.ProductId == productId)
            .Join(
                DbContext.Sales.AsNoTracking().Where(sale => sale.Status == SaleStatus.Completed),
                item => EF.Property<Guid>(item, "SaleId"),
                sale => sale.Id,
                (_, _) => true)
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var normalized = barcode.Trim().ToUpperInvariant();
        return DbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.Barcode.Value.ToUpper() == normalized, cancellationToken);
    }

    private static IQueryable<Product> ApplyFilters(
        IQueryable<Product> query,
        string? searchTerm,
        bool? isActive,
        bool lowStockOnly,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? minQuantity,
        int? maxQuantity)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            query = query.Where(product =>
                product.Barcode.Value.ToUpper().Contains(term) ||
                (product.ISBN != null && product.ISBN.Value.ToUpper().Contains(term)) ||
                product.Title.ToUpper().Contains(term) ||
                (product.Author != null && product.Author.ToUpper().Contains(term)) ||
                (product.Publisher != null && product.Publisher.ToUpper().Contains(term)) ||
                (product.Category != null && product.Category.Name.ToUpper().Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(product => product.IsActive == isActive.Value);
        }

        if (lowStockOnly)
        {
            query = query.Where(product => product.Quantity <= product.MinimumStock);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(product => product.SellingPrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(product => product.SellingPrice <= maxPrice.Value);
        }

        if (minQuantity.HasValue)
        {
            query = query.Where(product => product.Quantity >= minQuantity.Value);
        }

        if (maxQuantity.HasValue)
        {
            query = query.Where(product => product.Quantity <= maxQuantity.Value);
        }

        return query;
    }
}
