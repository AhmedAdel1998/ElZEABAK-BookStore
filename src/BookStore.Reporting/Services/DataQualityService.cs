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

        // Each check reports a true count over the whole table and separately takes a bounded sample
        // for the grid. Counting the sample instead presented the page size as the total, so 900
        // products missing a supplier were reported as exactly 200.
        var missingBarcode = await CheckAsync(
            products.Where(product => product.Barcode == null || product.Barcode.Value == string.Empty),
            "High", "Products", "Missing barcode", "Assign a unique barcode before production checkout.",
            maxIssues, cancellationToken);

        var missingSupplier = await CheckAsync(
            products.Where(product => !_dbContext.ProductSuppliers.Any(link => link.ProductId == product.Id)),
            "Medium", "Products", "Missing supplier", "Connect the product to at least one supplier.",
            maxIssues, cancellationToken);

        var invalidPrice = await CheckAsync(
            products.Where(product => product.PurchasePrice < 0 || product.SellingPrice <= 0 || product.SellingPrice < product.PurchasePrice),
            "High", "Pricing", "Invalid price", "Review purchase and selling prices.",
            maxIssues, cancellationToken);

        var lowMargin = await CheckAsync(
            products.Where(product => product.SellingPrice > 0 && product.SellingPrice >= product.PurchasePrice && ((product.SellingPrice - product.PurchasePrice) / product.SellingPrice) * 100 < minimumMargin),
            "Medium", "Pricing", "Low margin", $"Review margin below {minimumMargin:N0}%.",
            maxIssues, cancellationToken);

        var negativeStock = await CheckAsync(
            products.Where(product => product.Quantity < 0),
            "High", "Inventory", "Negative stock", "Reconcile stock through an inventory adjustment.",
            maxIssues, cancellationToken);

        var inactiveWithStock = await CheckAsync(
            products.Where(product => !product.IsActive && product.Quantity > 0),
            "Low", "Inventory", "Inactive product has stock", "Activate the product or clear remaining stock.",
            maxIssues, cancellationToken);

        var checks = new[] { missingBarcode, missingSupplier, invalidPrice, lowMargin, negativeStock, inactiveWithStock };

        // Severest first, so truncating the list can never hide a High finding behind a flood of
        // Medium ones -- which is what buried invalid prices and negative stock.
        var issues = checks
            .SelectMany(check => check.Issues)
            .OrderBy(issue => SeverityRank(issue.Severity))
            .ThenBy(issue => issue.Area, StringComparer.Ordinal)
            .Take(maxIssues)
            .ToArray();

        return new DataQualitySummaryDto(
            checks.Sum(check => check.Count),
            missingBarcode.Count,
            missingSupplier.Count,
            invalidPrice.Count,
            lowMargin.Count,
            negativeStock.Count,
            inactiveWithStock.Count,
            issues);
    }

    /// <summary>
    /// Counts every row matching a check and materialises a bounded sample of them.
    /// </summary>
    private static async Task<(int Count, List<DataQualityIssueDto> Issues)> CheckAsync(
        IQueryable<Domain.Entities.Product> query,
        string severity,
        string area,
        string issue,
        string recommendation,
        int maxIssues,
        CancellationToken cancellationToken)
    {
        var count = await query.CountAsync(cancellationToken);
        var sample = await query
            .OrderBy(product => product.Title)
            .Select(product => new { product.Id, product.Title })
            .Take(maxIssues)
            .ToListAsync(cancellationToken);

        return (count, sample.Select(product => Issue(severity, area, issue, product.Id, product.Title, recommendation)).ToList());
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "High" => 0,
        "Medium" => 1,
        _ => 2
    };

    private static DataQualityIssueDto Issue(string severity, string area, string issue, Guid entityId, string entityName, string recommendation)
    {
        return new DataQualityIssueDto(severity, area, issue, entityId, entityName, recommendation);
    }
}
