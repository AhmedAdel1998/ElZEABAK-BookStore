using BookStore.Application.Features.DataQuality.DTOs;
using BookStore.Application.Features.DataQuality.Queries;
using BookStore.Application.Features.DataQuality.Services;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;

namespace BookStore.Application.Features.DataQuality.Handlers;

/// <summary>
/// Handles data quality summary requests.
/// </summary>
public sealed class GetDataQualitySummaryHandler
{
    private readonly IDataQualityService _dataQualityService;
    private readonly IAuthorizationService _authorizationService;

    /// <summary>Initializes a new instance of the <see cref="GetDataQualitySummaryHandler"/> class.</summary>
    public GetDataQualitySummaryHandler(IDataQualityService dataQualityService, IAuthorizationService authorizationService)
    {
        _dataQualityService = dataQualityService;
        _authorizationService = authorizationService;
    }

    /// <summary>Gets the data quality summary.</summary>
    public async Task<Result<DataQualitySummaryDto>> HandleAsync(GetDataQualitySummaryQuery query, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.DataQualityView))
        {
            return Result<DataQualitySummaryDto>.Failure("You do not have permission to view data quality checks.");
        }

        return Result<DataQualitySummaryDto>.Success(await _dataQualityService.GetSummaryAsync(query, cancellationToken));
    }
}
