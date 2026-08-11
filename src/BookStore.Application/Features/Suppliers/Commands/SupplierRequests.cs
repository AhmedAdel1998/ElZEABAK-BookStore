using BookStore.Application.Features.Suppliers.DTOs;

namespace BookStore.Application.Features.Suppliers.Commands.CreateSupplier
{
    /// <summary>Requests supplier creation.</summary>
    public sealed record CreateSupplierRequest(SupplierEditorModel Supplier);
}

namespace BookStore.Application.Features.Suppliers.Commands.UpdateSupplier
{
    /// <summary>Requests supplier update.</summary>
    public sealed record UpdateSupplierRequest(SupplierEditorModel Supplier);
}

namespace BookStore.Application.Features.Suppliers.Commands.DeleteSupplier
{
    /// <summary>Requests supplier soft deletion.</summary>
    public sealed record DeleteSupplierRequest(Guid Id);
}

namespace BookStore.Application.Features.Suppliers.Commands.ActivateSupplier
{
    /// <summary>Requests supplier activation.</summary>
    public sealed record ActivateSupplierRequest(Guid Id);
}

namespace BookStore.Application.Features.Suppliers.Commands.DeactivateSupplier
{
    /// <summary>Requests supplier deactivation.</summary>
    public sealed record DeactivateSupplierRequest(Guid Id);
}
