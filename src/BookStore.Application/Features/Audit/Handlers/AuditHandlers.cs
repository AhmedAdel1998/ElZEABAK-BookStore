using BookStore.Application.Features.Audit.DTOs;
using BookStore.Application.Features.Audit.Queries;
using BookStore.Application.Features.Audit.Services;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;

namespace BookStore.Application.Features.Audit.Handlers;

/// <summary>
/// Records production audit events.
/// </summary>
public sealed class RecordAuditEntryHandler
{
    private readonly IAuditTrailService _auditTrailService;

    /// <summary>Initializes a new instance of the <see cref="RecordAuditEntryHandler"/> class.</summary>
    public RecordAuditEntryHandler(IAuditTrailService auditTrailService)
    {
        _auditTrailService = auditTrailService;
    }

    /// <summary>Records an audit entry.</summary>
    public async Task<OperationResult> HandleAsync(AuditLogRequest request, CancellationToken cancellationToken = default)
    {
        await _auditTrailService.RecordAsync(request, cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>
/// Searches production audit events.
/// </summary>
public sealed class SearchAuditLogHandler
{
    private readonly IAuditTrailService _auditTrailService;
    private readonly IAuthorizationService _authorizationService;

    /// <summary>Initializes a new instance of the <see cref="SearchAuditLogHandler"/> class.</summary>
    public SearchAuditLogHandler(IAuditTrailService auditTrailService, IAuthorizationService authorizationService)
    {
        _auditTrailService = auditTrailService;
        _authorizationService = authorizationService;
    }

    /// <summary>Searches audit entries.</summary>
    public async Task<Result<PagedResult<AuditLogEntryDto>>> HandleAsync(SearchAuditLogQuery query, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.AuditView))
        {
            return Result<PagedResult<AuditLogEntryDto>>.Failure("You do not have permission to view audit logs.");
        }

        return Result<PagedResult<AuditLogEntryDto>>.Success(await _auditTrailService.SearchAsync(query, cancellationToken));
    }
}
