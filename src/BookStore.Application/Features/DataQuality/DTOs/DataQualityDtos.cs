namespace BookStore.Application.Features.DataQuality.DTOs;

/// <summary>
/// Summary of catalog data quality issues.
/// </summary>
public sealed record DataQualitySummaryDto(
    int TotalIssues,
    int MissingBarcodeCount,
    int MissingSupplierCount,
    int InvalidPriceCount,
    int LowMarginCount,
    int NegativeStockCount,
    int InactiveWithStockCount,
    IReadOnlyCollection<DataQualityIssueDto> Issues);

/// <summary>
/// A single data quality issue.
/// </summary>
public sealed record DataQualityIssueDto(
    string Severity,
    string Area,
    string Issue,
    Guid? EntityId,
    string? EntityName,
    string Recommendation);
