using BookStore.Application.Features.Audit.DTOs;
using BookStore.Application.Features.Audit.Queries;
using BookStore.Application.Features.Audit.Services;
using BookStore.Domain.Entities;
using BookStore.Persistence.Context;
using BookStore.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Reporting.Services;

/// <summary>
/// EF-backed audit trail service.
/// </summary>
public sealed class AuditTrailService : IAuditTrailService
{
    private readonly BookStoreDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="AuditTrailService"/> class.</summary>
    public AuditTrailService(BookStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task RecordAsync(AuditLogRequest request, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogEntries.AddAsync(new AuditLogEntry(
            request.Area,
            request.Action,
            request.Outcome,
            request.UserId,
            request.Username,
            request.EntityType,
            request.EntityId,
            request.Detail), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditLogEntryDto>> SearchAsync(SearchAuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 250);
        var entries = _dbContext.AuditLogEntries.AsNoTracking().AsQueryable();

        if (query.From.HasValue)
        {
            entries = entries.Where(entry => entry.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            entries = entries.Where(entry => entry.OccurredAt <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            entries = entries.Where(entry => entry.Area == query.Area.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim();
            entries = entries.Where(entry =>
                entry.Action.Contains(search) ||
                (entry.Username != null && entry.Username.Contains(search)) ||
                (entry.EntityType != null && entry.EntityType.Contains(search)) ||
                (entry.Detail != null && entry.Detail.Contains(search)));
        }

        var total = await entries.CountAsync(cancellationToken);
        var rows = await entries
            .OrderByDescending(entry => entry.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new AuditLogEntryDto(
                entry.Id,
                entry.OccurredAt,
                entry.Area,
                entry.Action,
                entry.Outcome,
                entry.UserId,
                entry.Username,
                entry.EntityType,
                entry.EntityId,
                entry.Detail))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogEntryDto> { Items = rows, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }
}
