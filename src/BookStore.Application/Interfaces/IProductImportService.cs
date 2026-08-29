using BookStore.Application.Features.Products.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Defines product import operations.
/// </summary>
public interface IProductImportService
{
    /// <summary>
    /// Imports product editor rows from a file.
    /// </summary>
    Task<IReadOnlyCollection<ProductEditorModel>> ImportAsync(string filePath, CancellationToken cancellationToken = default);
}
