using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.Specifications;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework role repository.
/// </summary>
public class RoleRepository(BookStoreDbContext dbContext) : Repository<Role>(dbContext), IRoleRepository
{
    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Role>> ListAsync(ISpecification<Role>? specification = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Role> query = DbContext.Roles.AsNoTracking().Include(role => role.Permissions);

        if (specification?.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
