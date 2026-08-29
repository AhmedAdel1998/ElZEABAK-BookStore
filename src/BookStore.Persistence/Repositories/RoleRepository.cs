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
    public override Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Roles.Include(role => role.Permissions).FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
    }

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

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Permissions.OrderBy(permission => permission.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, Guid? excludedRoleId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        return DbContext.Roles.AsNoTracking().AnyAsync(role => role.Name.ToUpper() == normalized && (!excludedRoleId.HasValue || role.Id != excludedRoleId.Value), cancellationToken);
    }
}
