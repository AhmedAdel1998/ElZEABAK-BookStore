using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.Specifications;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework sale repository.
/// </summary>
public class SaleRepository(BookStoreDbContext dbContext) : Repository<Sale>(dbContext), ISaleRepository
{
    /// <inheritdoc />
    public async Task<Sale?> GetCompletedWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ReceiptDetailsQuery()
            .FirstOrDefaultAsync(sale => sale.Id == id && sale.Status == Domain.Enums.SaleStatus.Completed, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Sale?> GetCompletedByInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await ReceiptDetailsQuery()
            .FirstOrDefaultAsync(sale => sale.InvoiceNumber == invoiceNumber && sale.Status == Domain.Enums.SaleStatus.Completed, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Sale>> ListAsync(ISpecification<Sale>? specification = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Sale> query = DbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Customer)
            .Include(sale => sale.Cashier)
            .Include(sale => sale.SaleItems);

        if (specification?.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Sale>> SearchCompletedAsync(string? invoiceNumber, DateTimeOffset? date, Guid? cashierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<Sale> query = DbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Cashier)
            .Where(sale => sale.Status == Domain.Enums.SaleStatus.Completed);

        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            var search = invoiceNumber.Trim();
            query = query.Where(sale => sale.InvoiceNumber.Contains(search));
        }

        if (date.HasValue)
        {
            var start = date.Value.ToUniversalTime().Date;
            var end = start.AddDays(1);
            query = query.Where(sale => sale.SaleDate >= start && sale.SaleDate < end);
        }

        if (cashierId.HasValue)
        {
            query = query.Where(sale => sale.UserId == cashierId.Value);
        }

        return await query
            .OrderByDescending(sale => sale.SaleDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<Sale> ReceiptDetailsQuery()
    {
        return DbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Customer)
            .Include(sale => sale.Cashier)
            .Include(sale => sale.SaleItems)
            .ThenInclude(item => item.Product);
    }
}
