using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Inventory.Commands.AdjustStock;

/// <summary>
/// Requests a manual stock adjustment to a target quantity.
/// </summary>
public sealed record AdjustStockRequest(Guid ProductId, int TargetQuantity, InventoryTransactionType TransactionType, string Reason, string? Reference = null, string? Notes = null);
