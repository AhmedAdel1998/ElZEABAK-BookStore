using BookStore.Application.Features.Products.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Defines product export operations.
/// </summary>
public interface IProductExportService
{
    /// <summary>
    /// Exports products to a target file.
    /// </summary>
    Task ExportAsync(IEnumerable<ProductListItem> products, string filePath, CancellationToken cancellationToken = default);
}
