namespace BookStore.Application.Features.DataQuality.Queries;

/// <summary>
/// Gets local production data quality checks.
/// </summary>
public sealed record GetDataQualitySummaryQuery(decimal MinimumMarginPercent = 10, int MaxIssues = 200);
