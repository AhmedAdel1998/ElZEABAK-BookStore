using BookStore.Application.Features.Customers.DTOs;

namespace BookStore.Application.Features.Customers.Commands.CreateCustomer
{
    /// <summary>Requests customer creation.</summary>
    public sealed record CreateCustomerRequest(CustomerEditorModel Customer);
}

namespace BookStore.Application.Features.Customers.Commands.UpdateCustomer
{
    /// <summary>Requests customer update.</summary>
    public sealed record UpdateCustomerRequest(CustomerEditorModel Customer);
}

namespace BookStore.Application.Features.Customers.Commands.DeleteCustomer
{
    /// <summary>Requests customer soft deletion.</summary>
    public sealed record DeleteCustomerRequest(Guid Id);
}

namespace BookStore.Application.Features.Customers.Commands.ActivateCustomer
{
    /// <summary>Requests customer activation.</summary>
    public sealed record ActivateCustomerRequest(Guid Id);
}

namespace BookStore.Application.Features.Customers.Commands.DeactivateCustomer
{
    /// <summary>Requests customer deactivation.</summary>
    public sealed record DeactivateCustomerRequest(Guid Id);
}
