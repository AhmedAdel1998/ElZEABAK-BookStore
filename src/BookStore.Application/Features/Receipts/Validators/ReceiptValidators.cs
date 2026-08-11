using BookStore.Application.Features.Receipts.Commands;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Queries;
using FluentValidation;

namespace BookStore.Application.Features.Receipts.Validators;

public sealed class PrintReceiptCommandValidator : AbstractValidator<PrintReceiptCommand>
{
    public PrintReceiptCommandValidator()
    {
        RuleFor(command => command.SaleId).NotEmpty();
        RuleFor(command => command.Copies).InclusiveBetween(1, 5);
    }
}

public sealed class ReprintReceiptCommandValidator : AbstractValidator<ReprintReceiptCommand>
{
    public ReprintReceiptCommandValidator()
    {
        RuleFor(command => command.InvoiceNumber).NotEmpty().MaximumLength(50);
        RuleFor(command => command.Copies).InclusiveBetween(1, 5);
    }
}

public sealed class TestPrintCommandValidator : AbstractValidator<TestPrintCommand>
{
    public TestPrintCommandValidator() => RuleFor(command => command.PrinterName).MaximumLength(250);
}

public sealed class GetReceiptPreviewQueryValidator : AbstractValidator<GetReceiptPreviewQuery>
{
    public GetReceiptPreviewQueryValidator() => RuleFor(query => query.InvoiceNumber).NotEmpty().MaximumLength(50);
}

public sealed class SearchReceiptSalesQueryValidator : AbstractValidator<SearchReceiptSalesQuery>
{
    public SearchReceiptSalesQueryValidator()
    {
        RuleFor(query => query.InvoiceNumber).MaximumLength(50);
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200);
    }
}

public sealed class ReceiptModelValidator : AbstractValidator<ReceiptModel>
{
    public ReceiptModelValidator()
    {
        RuleFor(receipt => receipt.InvoiceNumber).NotEmpty();
        RuleFor(receipt => receipt.StoreName).NotEmpty();
        RuleFor(receipt => receipt.Cashier).NotEmpty();
        RuleFor(receipt => receipt.Items).NotEmpty();
        RuleFor(receipt => receipt.GrandTotal).GreaterThanOrEqualTo(0);
        RuleForEach(receipt => receipt.Items).ChildRules(item =>
        {
            item.RuleFor(row => row.ProductName).NotEmpty();
            item.RuleFor(row => row.Quantity).GreaterThan(0);
            item.RuleFor(row => row.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(row => row.LineTotal).GreaterThanOrEqualTo(0);
        });
    }
}
