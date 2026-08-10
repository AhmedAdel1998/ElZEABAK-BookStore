using BookStore.Application.Features.Customers.DTOs;

namespace BookStore.Application.Features.Customers.Queries.GetCustomers
{
    /// <summary>Requests a page of customers.</summary>
    public sealed record GetCustomersRequest(int PageNumber = 1, int PageSize = 25);
}

namespace BookStore.Application.Features.Customers.Queries.SearchCustomers
{
    /// <summary>Requests filtered customer search.</summary>
    public sealed record SearchCustomersRequest(CustomerFilter Filter);
}

namespace BookStore.Application.Features.Customers.Queries.GetCustomerById
{
    /// <summary>Requests a customer by identifier.</summary>
    public sealed record GetCustomerByIdRequest(Guid Id);
}

namespace BookStore.Application.Features.Customers.Queries.GetCustomerSalesHistory
{
    /// <summary>Requests customer sales history.</summary>
    public sealed record GetCustomerSalesHistoryRequest(Guid CustomerId, DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null, int PageNumber = 1, int PageSize = 25);
}
