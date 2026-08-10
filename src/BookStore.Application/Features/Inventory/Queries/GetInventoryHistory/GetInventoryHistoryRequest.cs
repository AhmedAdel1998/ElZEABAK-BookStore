using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Inventory.Queries.GetInventoryHistory;

/// <summary>
/// Requests inventory transaction history.
/// </summary>
public sealed record GetInventoryHistoryRequest(Guid? ProductId = null, DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null, InventoryTransactionType? TransactionType = null, Guid? UserId = null, int PageNumber = 1, int PageSize = 25);
