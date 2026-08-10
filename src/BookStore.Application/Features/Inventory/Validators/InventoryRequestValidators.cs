using BookStore.Application.Features.Inventory.Commands.AdjustStock;
using BookStore.Application.Features.Inventory.Commands.DecreaseStock;
using BookStore.Application.Features.Inventory.Commands.IncreaseStock;
using BookStore.Application.Features.Inventory.Queries.GetInventory;
using BookStore.Application.Features.Inventory.Queries.GetInventoryHistory;
using BookStore.Application.Features.Inventory.Queries.GetLowStockProducts;
using BookStore.Application.Features.Inventory.Queries.GetOutOfStockProducts;
using BookStore.Domain.Enums;
using FluentValidation;

namespace BookStore.Application.Features.Inventory.Validators;

/// <summary>Validates stock increase requests.</summary>
public sealed class IncreaseStockRequestValidator : AbstractValidator<IncreaseStockRequest>
{
    /// <summary>Initializes a new instance of the <see cref="IncreaseStockRequestValidator"/> class.</summary>
    public IncreaseStockRequestValidator()
    {
        RuleFor(request => request.ProductId).NotEmpty().WithMessage("Product is required.");
        RuleFor(request => request.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(request => request.Reason).NotEmpty().WithMessage("Reason is required.").MaximumLength(250);
    }
}

/// <summary>Validates stock decrease requests.</summary>
public sealed class DecreaseStockRequestValidator : AbstractValidator<DecreaseStockRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DecreaseStockRequestValidator"/> class.</summary>
    public DecreaseStockRequestValidator()
    {
        RuleFor(request => request.ProductId).NotEmpty().WithMessage("Product is required.");
        RuleFor(request => request.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(request => request.Reason).NotEmpty().WithMessage("Reason is required.").MaximumLength(250);
    }
}

/// <summary>Validates stock adjustment requests.</summary>
public sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    /// <summary>Initializes a new instance of the <see cref="AdjustStockRequestValidator"/> class.</summary>
    public AdjustStockRequestValidator()
    {
        RuleFor(request => request.ProductId).NotEmpty().WithMessage("Product is required.");
        RuleFor(request => request.TargetQuantity).GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
        RuleFor(request => request.Reason).NotEmpty().WithMessage("Reason is required.").MaximumLength(250);
        RuleFor(request => request.TransactionType)
            .Must(type => type is InventoryTransactionType.ManualAdjustment or InventoryTransactionType.Correction or InventoryTransactionType.InitialStock or InventoryTransactionType.Damage or InventoryTransactionType.Return)
            .WithMessage("Transaction type is required.");
    }
}

/// <summary>Validates inventory list requests.</summary>
public sealed class GetInventoryRequestValidator : AbstractValidator<GetInventoryRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetInventoryRequestValidator"/> class.</summary>
    public GetInventoryRequestValidator()
    {
        RuleFor(request => request.SearchTerm).MaximumLength(250);
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Validates inventory history requests.</summary>
public sealed class GetInventoryHistoryRequestValidator : AbstractValidator<GetInventoryHistoryRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetInventoryHistoryRequestValidator"/> class.</summary>
    public GetInventoryHistoryRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Validates low-stock inventory requests.</summary>
public sealed class GetLowStockProductsRequestValidator : AbstractValidator<GetLowStockProductsRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetLowStockProductsRequestValidator"/> class.</summary>
    public GetLowStockProductsRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Validates out-of-stock inventory requests.</summary>
public sealed class GetOutOfStockProductsRequestValidator : AbstractValidator<GetOutOfStockProductsRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetOutOfStockProductsRequestValidator"/> class.</summary>
    public GetOutOfStockProductsRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}
