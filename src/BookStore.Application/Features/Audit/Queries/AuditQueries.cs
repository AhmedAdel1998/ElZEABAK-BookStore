namespace BookStore.Application.Features.Audit.Queries;

/// <summary>
/// Audit log search query.
/// </summary>
public sealed record SearchAuditLogQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Area = null,
    string? SearchText = null,
    int PageNumber = 1,
    int PageSize = 100);
