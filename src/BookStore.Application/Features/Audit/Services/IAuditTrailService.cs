using BookStore.Application.Features.Audit.DTOs;
using BookStore.Application.Features.Audit.Queries;
using BookStore.Shared.Results;

namespace BookStore.Application.Features.Audit.Services;

/// <summary>
/// Provides production audit trail writes and read-side queries.
/// </summary>
public interface IAuditTrailService
{
    /// <summary>Persists an audit event.</summary>
    Task RecordAsync(AuditLogRequest request, CancellationToken cancellationToken = default);

    /// <summary>Searches audit events.</summary>
    Task<PagedResult<AuditLogEntryDto>> SearchAsync(SearchAuditLogQuery query, CancellationToken cancellationToken = default);
}
