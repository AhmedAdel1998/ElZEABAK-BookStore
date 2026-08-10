using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Persistence.Context;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework supplier repository.
/// </summary>
public class SupplierRepository(BookStoreDbContext dbContext) : Repository<Supplier>(dbContext), ISupplierRepository
{
}
