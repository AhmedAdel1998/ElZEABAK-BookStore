using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Persistence.Repositories;

/// <summary>
/// Entity Framework inventory transaction repository.
/// </summary>
public class InventoryRepository(BookStoreDbContext dbContext) : Repository<InventoryTransaction>(dbContext), IInventoryRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<InventoryTransaction>> SearchHistoryAsync(
        Guid? productId,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        InventoryTransactionType? transactionType,
        Guid? userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(DbContext.InventoryTransactions.AsNoTracking(), productId, dateFrom, dateTo, transactionType, userId)
            .OrderByDescending(transaction => transaction.Date)
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Max(pageSize, 1))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountHistoryAsync(
        Guid? productId = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        InventoryTransactionType? transactionType = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        return ApplyFilters(DbContext.InventoryTransactions.AsNoTracking(), productId, dateFrom, dateTo, transactionType, userId)
            .CountAsync(cancellationToken);
    }

    private static IQueryable<InventoryTransaction> ApplyFilters(
        IQueryable<InventoryTransaction> query,
        Guid? productId,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        InventoryTransactionType? transactionType,
        Guid? userId)
    {
        if (productId.HasValue)
        {
            query = query.Where(transaction => transaction.ProductId == productId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(transaction => transaction.Date >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(transaction => transaction.Date <= dateTo.Value);
        }

        if (transactionType.HasValue)
        {
            query = query.Where(transaction => transaction.TransactionType == transactionType.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(transaction => transaction.UserId == userId.Value);
        }

        return query;
    }
}
