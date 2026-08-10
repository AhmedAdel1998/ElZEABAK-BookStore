using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Sales.Commands.StartSale
{
    /// <summary>Requests starting a new POS sale.</summary>
    public sealed record StartSaleRequest(string CustomerName = "Walk-in Customer");
}

namespace BookStore.Application.Features.Sales.Commands.AddItem
{
    /// <summary>Requests adding an item to cart.</summary>
    public sealed record AddItemRequest(Guid ProductId, int Quantity = 1);
}

namespace BookStore.Application.Features.Sales.Commands.UpdateItemQuantity
{
    /// <summary>Requests cart item quantity update.</summary>
    public sealed record UpdateItemQuantityRequest(Guid ItemId, int Quantity);
}

namespace BookStore.Application.Features.Sales.Commands.RemoveItem
{
    /// <summary>Requests cart item removal.</summary>
    public sealed record RemoveItemRequest(Guid ItemId);
}

namespace BookStore.Application.Features.Sales.Commands.ApplyLineDiscount
{
    /// <summary>Requests line discount application.</summary>
    public sealed record ApplyLineDiscountRequest(Guid ItemId, decimal Discount);
}

namespace BookStore.Application.Features.Sales.Commands.ApplyInvoiceDiscount
{
    /// <summary>Requests invoice discount application.</summary>
    public sealed record ApplyInvoiceDiscountRequest(decimal Discount);
}

namespace BookStore.Application.Features.Sales.Commands.CancelSale
{
    /// <summary>Requests sale cancellation.</summary>
    public sealed record CancelSaleRequest(string Reason);
}

namespace BookStore.Application.Features.Sales.Commands.SuspendSale
{
    /// <summary>Requests sale suspension.</summary>
    public sealed record SuspendSaleRequest;
}

namespace BookStore.Application.Features.Sales.Commands.ResumeSale
{
    /// <summary>Requests suspended sale resume.</summary>
    public sealed record ResumeSaleRequest(Guid SaleId);
}

namespace BookStore.Application.Features.Sales.Commands.CompleteSale
{
    /// <summary>Requests sale completion.</summary>
    public sealed record CompleteSaleRequest(PaymentMethod PaymentMethod, decimal AmountPaid);
}

namespace BookStore.Application.Features.Sales.Commands.SelectCustomer
{
    /// <summary>Requests selecting a customer for the active POS sale.</summary>
    public sealed record SelectCustomerForSaleRequest(Guid CustomerId);
}

namespace BookStore.Application.Features.Sales.Commands.ClearCustomer
{
    /// <summary>Requests clearing the selected customer from the active POS sale.</summary>
    public sealed record ClearCustomerFromSaleRequest;
}
