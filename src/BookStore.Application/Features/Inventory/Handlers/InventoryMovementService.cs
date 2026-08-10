using BookStore.Application.Features.Inventory.Responses;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Inventory.Handlers;

/// <summary>
/// Applies inventory movements and keeps product quantity synchronized with ledger entries.
/// </summary>
public sealed class InventoryMovementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InventoryMovementService> _logger;

    public InventoryMovementService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<InventoryMovementService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<InventoryMovementResponse>> MoveAsync(Guid productId, int quantityChange, InventoryTransactionType transactionType, string reason, string? reference, string? notes, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result<InventoryMovementResponse>.Failure("Product was not found.");
        }

        if (!product.IsActive)
        {
            return Result<InventoryMovementResponse>.Failure("Cannot adjust inactive products.");
        }

        var before = product.Quantity;
        var after = before + quantityChange;
        if (after < 0)
        {
            return Result<InventoryMovementResponse>.Failure("Stock cannot become negative.");
        }

        product.SetQuantity(after);
        var transaction = new InventoryTransaction(
            product.Id,
            quantityChange,
            transactionType,
            before,
            after,
            reason,
            reference,
            _currentUserService.UserId,
            _currentUserService.FullName ?? _currentUserService.Username,
            notes);

        await _unitOfWork.Inventory.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Inventory movement {Type}: User={User} Product={ProductId} OldQuantity={OldQuantity} NewQuantity={NewQuantity} Reason={Reason} Timestamp={Timestamp}",
            transactionType,
            _currentUserService.Username,
            product.Id,
            before,
            after,
            reason,
            DateTimeOffset.UtcNow);

        return Result<InventoryMovementResponse>.Success(new InventoryMovementResponse
        {
            TransactionId = transaction.Id,
            ProductId = product.Id,
            ProductTitle = product.Title,
            TransactionType = transactionType,
            QuantityBefore = before,
            QuantityAfter = after,
            QuantityChange = quantityChange
        });
    }
}
