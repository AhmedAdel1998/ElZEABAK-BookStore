using BookStore.Application.Features.Suppliers.DTOs;

namespace BookStore.Application.Features.Suppliers.Queries.GetSuppliers
{
    /// <summary>Requests a page of suppliers.</summary>
    public sealed record GetSuppliersRequest(int PageNumber = 1, int PageSize = 25);
}

namespace BookStore.Application.Features.Suppliers.Queries.SearchSuppliers
{
    /// <summary>Requests filtered supplier search.</summary>
    public sealed record SearchSuppliersRequest(SupplierFilter Filter);
}

namespace BookStore.Application.Features.Suppliers.Queries.GetSupplierById
{
    /// <summary>Requests a supplier by identifier.</summary>
    public sealed record GetSupplierByIdRequest(Guid Id);
}

namespace BookStore.Application.Features.Suppliers.Queries.GetSupplierProducts
{
    /// <summary>Requests products associated with a supplier.</summary>
    public sealed record GetSupplierProductsRequest(Guid SupplierId, int PageNumber = 1, int PageSize = 25);
}
