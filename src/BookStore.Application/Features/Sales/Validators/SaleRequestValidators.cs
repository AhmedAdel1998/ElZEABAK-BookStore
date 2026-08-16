using BookStore.Application.Features.Sales.Commands.AddItem;
using BookStore.Application.Features.Sales.Commands.ApplyInvoiceDiscount;
using BookStore.Application.Features.Sales.Commands.ApplyLineDiscount;
using BookStore.Application.Features.Sales.Commands.CancelSale;
using BookStore.Application.Features.Sales.Commands.CompleteSale;
using BookStore.Application.Features.Sales.Commands.RemoveItem;
using BookStore.Application.Features.Sales.Commands.ResumeSale;
using BookStore.Application.Features.Sales.Commands.SelectCustomer;
using BookStore.Application.Features.Sales.Commands.StartSale;
using BookStore.Application.Features.Sales.Commands.UpdateItemQuantity;
using BookStore.Application.Features.Sales.Queries.SearchProduct;
using FluentValidation;

namespace BookStore.Application.Features.Sales.Validators;

/// <summary>Validates POS request records.</summary>
public sealed class StartSaleRequestValidator : AbstractValidator<StartSaleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="StartSaleRequestValidator"/> class.</summary>
    public StartSaleRequestValidator() => RuleFor(request => request.CustomerName).NotEmpty().MaximumLength(150);
}

/// <summary>Validates add item requests.</summary>
public sealed class AddItemRequestValidator : AbstractValidator<AddItemRequest>
{
    /// <summary>Initializes a new instance of the <see cref="AddItemRequestValidator"/> class.</summary>
    public AddItemRequestValidator()
    {
        RuleFor(request => request.ProductId).NotEmpty();
        RuleFor(request => request.Quantity).GreaterThan(0);
    }
}

/// <summary>Validates quantity update requests.</summary>
public sealed class UpdateItemQuantityRequestValidator : AbstractValidator<UpdateItemQuantityRequest>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateItemQuantityRequestValidator"/> class.</summary>
    public UpdateItemQuantityRequestValidator()
    {
        RuleFor(request => request.ItemId).NotEmpty();
        RuleFor(request => request.Quantity).GreaterThan(0);
    }
}

/// <summary>Validates remove item requests.</summary>
public sealed class RemoveItemRequestValidator : AbstractValidator<RemoveItemRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RemoveItemRequestValidator"/> class.</summary>
    public RemoveItemRequestValidator() => RuleFor(request => request.ItemId).NotEmpty();
}

/// <summary>Validates line discount requests.</summary>
public sealed class ApplyLineDiscountRequestValidator : AbstractValidator<ApplyLineDiscountRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ApplyLineDiscountRequestValidator"/> class.</summary>
    public ApplyLineDiscountRequestValidator()
    {
        RuleFor(request => request.ItemId).NotEmpty();
        RuleFor(request => request.Discount).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Validates invoice discount requests.</summary>
public sealed class ApplyInvoiceDiscountRequestValidator : AbstractValidator<ApplyInvoiceDiscountRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ApplyInvoiceDiscountRequestValidator"/> class.</summary>
    public ApplyInvoiceDiscountRequestValidator() => RuleFor(request => request.Discount).GreaterThanOrEqualTo(0);
}

/// <summary>Validates cancel sale requests.</summary>
public sealed class CancelSaleRequestValidator : AbstractValidator<CancelSaleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CancelSaleRequestValidator"/> class.</summary>
    public CancelSaleRequestValidator() => RuleFor(request => request.Reason).NotEmpty().MaximumLength(250);
}

/// <summary>Validates resume sale requests.</summary>
public sealed class ResumeSaleRequestValidator : AbstractValidator<ResumeSaleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ResumeSaleRequestValidator"/> class.</summary>
    public ResumeSaleRequestValidator() => RuleFor(request => request.SaleId).NotEmpty();
}

/// <summary>Validates complete sale requests.</summary>
public sealed class CompleteSaleRequestValidator : AbstractValidator<CompleteSaleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CompleteSaleRequestValidator"/> class.</summary>
    public CompleteSaleRequestValidator()
    {
        RuleFor(request => request.AmountPaid).GreaterThanOrEqualTo(0);
        RuleFor(request => request.ReceiptCopies).InclusiveBetween(1, 5);
    }
}

/// <summary>Validates POS product search requests.</summary>
public sealed class SearchProductRequestValidator : AbstractValidator<SearchProductRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SearchProductRequestValidator"/> class.</summary>
    public SearchProductRequestValidator()
    {
        RuleFor(request => request.SearchTerm).NotEmpty().MaximumLength(250);
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Validates POS customer selection requests.</summary>
public sealed class SelectCustomerForSaleRequestValidator : AbstractValidator<SelectCustomerForSaleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SelectCustomerForSaleRequestValidator"/> class.</summary>
    public SelectCustomerForSaleRequestValidator() => RuleFor(request => request.CustomerId).NotEmpty();
}
