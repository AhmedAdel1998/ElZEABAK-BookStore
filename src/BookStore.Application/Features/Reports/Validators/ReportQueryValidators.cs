using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Queries;
using FluentValidation;

namespace BookStore.Application.Features.Reports.Validators;

public sealed class ReportDateRangeValidator : AbstractValidator<ReportDateRange>
{
    public ReportDateRangeValidator()
    {
        RuleFor(range => range.StartDate).LessThanOrEqualTo(range => range.EndDate).WithMessage("Start date must not be after end date.");
        RuleFor(range => range.EndDate).GreaterThan(range => range.StartDate).WithMessage("End date must be after start date.");
    }
}

public sealed class GetReportsDashboardQueryValidator : AbstractValidator<GetReportsDashboardQuery>
{
    public GetReportsDashboardQueryValidator() => RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
}

public sealed class GetSalesSummaryQueryValidator : AbstractValidator<GetSalesSummaryQuery>
{
    public GetSalesSummaryQueryValidator() => RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
}

public sealed class GetSalesDetailsQueryValidator : AbstractValidator<GetSalesDetailsQuery>
{
    public GetSalesDetailsQueryValidator()
    {
        RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
        RuleFor(query => query.SearchTerm).MaximumLength(100);
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200);
    }
}

public sealed class GetProfitReportQueryValidator : AbstractValidator<GetProfitReportQuery>
{
    public GetProfitReportQueryValidator() => RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
}

public sealed class GetBestSellingProductsQueryValidator : AbstractValidator<GetBestSellingProductsQuery>
{
    public GetBestSellingProductsQueryValidator()
    {
        RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
        RuleFor(query => query.Limit).InclusiveBetween(1, 200);
    }
}

public sealed class GetProductSalesQueryValidator : AbstractValidator<GetProductSalesQuery>
{
    public GetProductSalesQueryValidator()
    {
        RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
        RuleFor(query => query.ProductId).NotEmpty();
    }
}

public sealed class GetCategorySalesQueryValidator : AbstractValidator<GetCategorySalesQuery>
{
    public GetCategorySalesQueryValidator() => RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
}

public sealed class GetInventoryReportQueryValidator : AbstractValidator<GetInventoryReportQuery>
{
    public GetInventoryReportQueryValidator()
    {
        RuleFor(query => query.SearchTerm).MaximumLength(100);
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 500);
    }
}

public sealed class GetInventoryMovementsQueryValidator : AbstractValidator<GetInventoryMovementsQuery>
{
    public GetInventoryMovementsQueryValidator()
    {
        RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 500);
    }
}

public sealed class GetLowStockQueryValidator : AbstractValidator<GetLowStockQuery>
{
    public GetLowStockQueryValidator()
    {
        RuleFor(query => query.SearchTerm).MaximumLength(100);
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 500);
    }
}

public sealed class GetCustomerReportQueryValidator : AbstractValidator<GetCustomerReportQuery>
{
    public GetCustomerReportQueryValidator()
    {
        RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
        RuleFor(query => query.SearchTerm).MaximumLength(100);
        RuleFor(query => query.Top).GreaterThan(0).When(query => query.Top.HasValue);
    }
}

public sealed class GetCashierPerformanceQueryValidator : AbstractValidator<GetCashierPerformanceQuery>
{
    public GetCashierPerformanceQueryValidator() => RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
}

public sealed class GetPaymentMethodsQueryValidator : AbstractValidator<GetPaymentMethodsQuery>
{
    public GetPaymentMethodsQueryValidator() => RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
}

public sealed class GetDailySalesQueryValidator : AbstractValidator<GetDailySalesQuery>
{
    public GetDailySalesQueryValidator() => RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
}

public sealed class GetHourlySalesQueryValidator : AbstractValidator<GetHourlySalesQuery>
{
    public GetHourlySalesQueryValidator() => RuleFor(query => query.DateRange).SetValidator(new ReportDateRangeValidator());
}
