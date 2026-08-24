namespace BookStore.Application.Features.Sales.Queries.SearchProduct
{
    /// <summary>Requests product search for POS.</summary>
    public sealed record SearchProductRequest(string SearchTerm, int PageNumber = 1, int PageSize = 25);
}

namespace BookStore.Application.Features.Sales.Queries.GetCurrentSale
{
    /// <summary>Requests current POS sale.</summary>
    public sealed record GetCurrentSaleRequest;
}

namespace BookStore.Application.Features.Sales.Queries.GetHeldSales
{
    /// <summary>Requests held POS sales.</summary>
    public sealed record GetHeldSalesRequest;
}

namespace BookStore.Application.Features.Sales.Queries.GetSaleSummary
{
    /// <summary>Requests current sale summary.</summary>
    public sealed record GetSaleSummaryRequest;
}

namespace BookStore.Application.Features.Sales.Queries.GetOpenInvoices
{
    /// <summary>Requests every POS invoice that is currently open on this workstation.</summary>
    public sealed record GetOpenInvoicesRequest;
}
