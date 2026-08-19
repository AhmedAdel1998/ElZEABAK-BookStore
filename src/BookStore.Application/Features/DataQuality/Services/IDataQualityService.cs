using BookStore.Application.Features.DataQuality.DTOs;
using BookStore.Application.Features.DataQuality.Queries;

namespace BookStore.Application.Features.DataQuality.Services;

/// <summary>
/// Provides production data quality checks.
/// </summary>
public interface IDataQualityService
{
    /// <summary>Gets a data quality summary.</summary>
    Task<DataQualitySummaryDto> GetSummaryAsync(GetDataQualitySummaryQuery query, CancellationToken cancellationToken = default);
}
