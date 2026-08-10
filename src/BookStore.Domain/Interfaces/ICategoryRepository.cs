using BookStore.Domain.Entities;
using BookStore.Domain.Specifications;

namespace BookStore.Domain.Interfaces;

/// <summary>
/// Defines category persistence operations.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets a category by identifier.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The category when found; otherwise, <see langword="null"/>.</returns>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets categories matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching categories.</returns>
    Task<IReadOnlyCollection<Category>> ListAsync(ISpecification<Category>? specification = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a category.
    /// </summary>
    /// <param name="category">The category to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Searches categories by name or description.
    /// </summary>
    /// <param name="searchTerm">The optional search term.</param>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching categories.</returns>
    Task<IReadOnlyCollection<Category>> SearchAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts categories matching an optional search term.
    /// </summary>
    /// <param name="searchTerm">The optional search term.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching category count.</returns>
    Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a category name already exists.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="excludedCategoryId">An optional category identifier to exclude.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> when the name exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludedCategoryId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of products assigned to a category.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The assigned product count.</returns>
    Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product counts for categories.
    /// </summary>
    /// <param name="categoryIds">The category identifiers.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>Product counts keyed by category identifier.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default);
}
