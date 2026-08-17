using BookStore.Application.Features.DataQuality.DTOs;
using BookStore.Application.Features.DataQuality.Queries;
using BookStore.Application.Features.DataQuality.Services;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Reporting.Services;

/// <summary>
/// EF-backed data quality checks for production operations.
/// </summary>
public sealed class DataQualityService : IDataQualityService
{
    private readonly BookStoreDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="DataQualityService"/> class.</summary>
    public DataQualityService(BookStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<DataQualitySummaryDto> GetSummaryAsync(GetDataQualitySummaryQuery query, CancellationToken cancellationToken = default)
    {
        var minimumMargin = Math.Max(0, query.MinimumMarginPercent);
        var maxIssues = Math.Clamp(query.MaxIssues, 1, 500);
        var products = _dbContext.Products.AsNoTracking();
        var issues = new List<DataQualityIssueDto>();

        var missingBarcode = await products
            .Where(product => product.Barcode == null || product.Barcode.Value == string.Empty)
            .Select(product => new { product.Id, product.Title })
            .Take(maxIssues)
            .ToListAsync(cancellationToken);

        issues.AddRange(missingBarcode.Select(product => Issue("High", "Products", "Missing barcode", product.Id, product.Title, "Assign a unique barcode before production checkout.")));

        var missingSupplier = await products
            .Where(product => !_dbContext.ProductSuppliers.Any(link => link.ProductId == product.Id))
            .Select(product => new { product.Id, product.Title })
            .Take(maxIssues)
            .ToListAsync(cancellationToken);

        issues.AddRange(missingSupplier.Select(product => Issue("Medium", "Products", "Missing supplier", product.Id, product.Title, "Connect the product to at least one supplier.")));

        var invalidPrice = await products
            .Where(product => product.PurchasePrice < 0 || product.SellingPrice <= 0 || product.SellingPrice < product.PurchasePrice)
            .Select(product => new { product.Id, product.Title })
            .Take(maxIssues)
            .ToListAsync(cancellationToken);

        issues.AddRange(invalidPrice.Select(product => Issue("High", "Pricing", "Invalid price", product.Id, product.Title, "Review purchase and selling prices.")));

        var lowMargin = await products
            .Where(product => product.SellingPrice > 0 && product.SellingPrice >= product.PurchasePrice && ((product.SellingPrice - product.PurchasePrice) / product.SellingPrice) * 100 < minimumMargin)
            .Select(product => new { product.Id, product.Title })
            .Take(maxIssues)
            .ToListAsync(cancellationToken);

        issues.AddRange(lowMargin.Select(product => Issue("Medium", "Pricing", "Low margin", product.Id, product.Title, $"Review margin below {minimumMargin:N0}%.")));

        var negativeStock = await products
            .Where(product => product.Quantity < 0)
            .Select(product => new { product.Id, product.Title })
            .Take(maxIssues)
            .ToListAsync(cancellationToken);

        issues.AddRange(negativeStock.Select(product => Issue("High", "Inventory", "Negative stock", product.Id, product.Title, "Reconcile stock through an inventory adjustment.")));

        var inactiveWithStock = await products
            .Where(product => !product.IsActive && product.Quantity > 0)
            .Select(product => new { product.Id, product.Title })
            .Take(maxIssues)
            .ToListAsync(cancellationToken);

        issues.AddRange(inactiveWithStock.Select(product => Issue("Low", "Inventory", "Inactive product has stock", product.Id, product.Title, "Activate the product or clear remaining stock.")));

        return new DataQualitySummaryDto(
            issues.Count,
            missingBarcode.Count,
            missingSupplier.Count,
            invalidPrice.Count,
            lowMargin.Count,
            negativeStock.Count,
            inactiveWithStock.Count,
            issues.Take(maxIssues).ToArray());
    }

    private static DataQualityIssueDto Issue(string severity, string area, string issue, Guid entityId, string entityName, string recommendation)
    {
        return new DataQualityIssueDto(severity, area, issue, entityId, entityName, recommendation);
    }
}
