using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework category repository.
/// </summary>
public class CategoryRepository(BookStoreDbContext dbContext) : Repository<Category>(dbContext), ICategoryRepository
{
    /// <inheritdoc />
    public override Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Category>> SearchAsync(string? searchTerm, int pageNumber, int pageSize, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = ApplySearch(DbContext.Categories.AsNoTracking(), searchTerm);
        if (isActive.HasValue)
        {
            query = query.Where(category => category.IsActive == isActive.Value);
        }

        query = query.OrderBy(category => category.Name);

        return await query
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Max(pageSize, 1))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        return ApplySearch(DbContext.Categories.AsNoTracking(), searchTerm).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, Guid? excludedCategoryId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();
        return DbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.Name.ToUpper() == normalizedName &&
                    (excludedCategoryId == null || category.Id != excludedCategoryId.Value),
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return DbContext.Products.AsNoTracking().CountAsync(product => product.CategoryId == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.ToArray();
        return await DbContext.Products
            .AsNoTracking()
            .Where(product => ids.Contains(product.CategoryId))
            .GroupBy(product => product.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CategoryId, item => item.Count, cancellationToken);
    }

    private static IQueryable<Category> ApplySearch(IQueryable<Category> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var normalizedTerm = searchTerm.Trim().ToUpperInvariant();
        return query.Where(category =>
            category.Name.ToUpper().Contains(normalizedTerm) ||
            (category.Description != null && category.Description.ToUpper().Contains(normalizedTerm)));
    }
}
