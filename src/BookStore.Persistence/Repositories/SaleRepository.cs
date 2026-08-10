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
}
