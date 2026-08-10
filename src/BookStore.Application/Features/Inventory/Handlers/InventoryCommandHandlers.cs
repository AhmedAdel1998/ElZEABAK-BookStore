using BookStore.Application.Features.Inventory.Commands.AdjustStock;
using BookStore.Application.Features.Inventory.Commands.DecreaseStock;
using BookStore.Application.Features.Inventory.Commands.IncreaseStock;
using BookStore.Application.Features.Inventory.Responses;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Inventory.Handlers;

/// <summary>Handles stock increase requests.</summary>
public sealed class IncreaseStockHandler
{
    private readonly IValidator<IncreaseStockRequest> _validator;
    private readonly InventoryMovementService _movementService;
    private readonly ILogger<IncreaseStockHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="IncreaseStockHandler"/> class.</summary>
    public IncreaseStockHandler(IValidator<IncreaseStockRequest> validator, InventoryMovementService movementService, ILogger<IncreaseStockHandler> logger)
    {
        _validator = validator;
        _movementService = movementService;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<InventoryMovementResponse>> HandleAsync(IncreaseStockRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Stock increase validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<InventoryMovementResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        return await _movementService.MoveAsync(request.ProductId, request.Quantity, InventoryTransactionType.ManualAdjustment, request.Reason, request.Reference, request.Notes, cancellationToken);
    }
}

/// <summary>Handles stock decrease requests.</summary>
public sealed class DecreaseStockHandler
{
    private readonly IValidator<DecreaseStockRequest> _validator;
    private readonly InventoryMovementService _movementService;
    private readonly ILogger<DecreaseStockHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DecreaseStockHandler"/> class.</summary>
    public DecreaseStockHandler(IValidator<DecreaseStockRequest> validator, InventoryMovementService movementService, ILogger<DecreaseStockHandler> logger)
    {
        _validator = validator;
        _movementService = movementService;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<InventoryMovementResponse>> HandleAsync(DecreaseStockRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Stock decrease validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<InventoryMovementResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        return await _movementService.MoveAsync(request.ProductId, -request.Quantity, InventoryTransactionType.ManualAdjustment, request.Reason, request.Reference, request.Notes, cancellationToken);
    }
}

/// <summary>Handles stock adjustment requests.</summary>
public sealed class AdjustStockHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AdjustStockRequest> _validator;
    private readonly InventoryMovementService _movementService;
    private readonly ILogger<AdjustStockHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="AdjustStockHandler"/> class.</summary>
    public AdjustStockHandler(IUnitOfWork unitOfWork, IValidator<AdjustStockRequest> validator, InventoryMovementService movementService, ILogger<AdjustStockHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _movementService = movementService;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<InventoryMovementResponse>> HandleAsync(AdjustStockRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Stock adjustment validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<InventoryMovementResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<InventoryMovementResponse>.Failure("Product was not found.");
        }

        var change = request.TargetQuantity - product.Quantity;
        if (change == 0)
        {
            return Result<InventoryMovementResponse>.Failure("Adjustment does not change stock.");
        }

        return await _movementService.MoveAsync(request.ProductId, change, request.TransactionType, request.Reason, request.Reference, request.Notes, cancellationToken);
    }
}
