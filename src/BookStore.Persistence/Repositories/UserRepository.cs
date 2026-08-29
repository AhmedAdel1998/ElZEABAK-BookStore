using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.Specifications;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework user repository.
/// </summary>
public class UserRepository(BookStoreDbContext dbContext) : Repository<User>(dbContext), IUserRepository
{
    /// <inheritdoc />
    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Users
            .Include(user => user.Role)
            .ThenInclude(role => role!.Permissions)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await DbContext.Users
            .Include(user => user.Role)
            .ThenInclude(role => role!.Permissions)
            .FirstOrDefaultAsync(user => user.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByUsernameAsync(string username, Guid? excludedUserId = null, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToUpperInvariant();
        return DbContext.Users.AsNoTracking().AnyAsync(user => user.Username.ToUpper() == normalized && (!excludedUserId.HasValue || user.Id != excludedUserId.Value), cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<User>> ListAsync(ISpecification<User>? specification = null, CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = DbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .ThenInclude(role => role!.Permissions);

        if (specification?.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
