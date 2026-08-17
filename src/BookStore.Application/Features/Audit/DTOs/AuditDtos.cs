namespace BookStore.Application.Features.Audit.DTOs;

/// <summary>
/// Audit log row shown in the production audit trail.
/// </summary>
public sealed record AuditLogEntryDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string Area,
    string Action,
    string Outcome,
    Guid? UserId,
    string? Username,
    string? EntityType,
    Guid? EntityId,
    string? Detail);

/// <summary>
/// Audit entry write model.
/// </summary>
public sealed record AuditLogRequest(
    string Area,
    string Action,
    string Outcome,
    Guid? UserId,
    string? Username,
    string? EntityType = null,
    Guid? EntityId = null,
    string? Detail = null);
