using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Sales.Commands.StartSale
{
    /// <summary>Requests starting a new POS sale.</summary>
    /// <param name="CustomerName">The initial customer name.</param>
    /// <param name="ForceNew">
    /// When set, a new invoice is opened alongside the invoices already open instead of returning
    /// the active one.
    /// </param>
    public sealed record StartSaleRequest(string CustomerName = "Walk-in Customer", bool ForceNew = false);
}

namespace BookStore.Application.Features.Sales.Commands.AddItem
{
    /// <summary>Requests adding an item to a cart.</summary>
    /// <param name="ProductId">The product to add.</param>
    /// <param name="Quantity">The quantity to add.</param>
    /// <param name="SaleId">The invoice to add to. The active invoice when omitted.</param>
    public sealed record AddItemRequest(Guid ProductId, int Quantity = 1, Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.UpdateItemQuantity
{
    /// <summary>Requests a cart item quantity update.</summary>
    /// <param name="ItemId">The cart line to update.</param>
    /// <param name="Quantity">The new quantity.</param>
    /// <param name="SaleId">The invoice that owns the line. The active invoice when omitted.</param>
    public sealed record UpdateItemQuantityRequest(Guid ItemId, int Quantity, Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.RemoveItem
{
    /// <summary>Requests cart item removal.</summary>
    /// <param name="ItemId">The cart line to remove.</param>
    /// <param name="SaleId">The invoice that owns the line. The active invoice when omitted.</param>
    public sealed record RemoveItemRequest(Guid ItemId, Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.ApplyLineDiscount
{
    /// <summary>Requests line discount application.</summary>
    /// <param name="ItemId">The cart line to discount.</param>
    /// <param name="Discount">The discount amount.</param>
    /// <param name="SaleId">The invoice that owns the line. The active invoice when omitted.</param>
    public sealed record ApplyLineDiscountRequest(Guid ItemId, decimal Discount, Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.ApplyInvoiceDiscount
{
    /// <summary>Requests invoice discount application.</summary>
    /// <param name="Discount">The discount amount.</param>
    /// <param name="SaleId">The invoice to discount. The active invoice when omitted.</param>
    public sealed record ApplyInvoiceDiscountRequest(decimal Discount, Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.CancelSale
{
    /// <summary>Requests sale cancellation.</summary>
    /// <param name="Reason">The cancellation reason.</param>
    /// <param name="SaleId">The invoice to cancel. The active invoice when omitted.</param>
    public sealed record CancelSaleRequest(string Reason, Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.SuspendSale
{
    /// <summary>Requests sale suspension.</summary>
    /// <param name="SaleId">The invoice to suspend. The active invoice when omitted.</param>
    public sealed record SuspendSaleRequest(Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.ResumeSale
{
    /// <summary>Requests suspended sale resume.</summary>
    /// <param name="SaleId">The held sale to re-open.</param>
    public sealed record ResumeSaleRequest(Guid SaleId);
}

namespace BookStore.Application.Features.Sales.Commands.CompleteSale
{
    /// <summary>Requests sale completion.</summary>
    /// <param name="PaymentMethod">The payment method.</param>
    /// <param name="AmountPaid">The amount handed over.</param>
    /// <param name="ReceiptCopies">The number of receipt copies to print.</param>
    /// <param name="SaleId">The invoice to complete. The active invoice when omitted.</param>
    public sealed record CompleteSaleRequest(PaymentMethod PaymentMethod, decimal AmountPaid, int ReceiptCopies = 1, Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.SelectCustomer
{
    /// <summary>Requests selecting a customer for a POS invoice.</summary>
    /// <param name="CustomerId">The customer to attach.</param>
    /// <param name="SaleId">The invoice to attach the customer to. The active invoice when omitted.</param>
    public sealed record SelectCustomerForSaleRequest(Guid CustomerId, Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.ClearCustomer
{
    /// <summary>Requests clearing the selected customer from a POS invoice.</summary>
    /// <param name="SaleId">The invoice to clear. The active invoice when omitted.</param>
    public sealed record ClearCustomerFromSaleRequest(Guid? SaleId = null);
}

namespace BookStore.Application.Features.Sales.Commands.SwitchInvoice
{
    /// <summary>Requests making an already open invoice the active one.</summary>
    /// <param name="SaleId">The open invoice to switch to.</param>
    public sealed record SwitchInvoiceRequest(Guid SaleId);
}

namespace BookStore.Application.Features.Sales.Commands.CloseInvoice
{
    /// <summary>Requests closing an open invoice without completing it.</summary>
    /// <param name="SaleId">The open invoice to close.</param>
    /// <param name="Reason">The reason recorded in the log.</param>
    public sealed record CloseInvoiceRequest(Guid SaleId, string Reason = "Closed by cashier");
}

namespace BookStore.Application.Features.Sales.Commands.SaveInvoiceDraft
{
    /// <summary>
    /// Requests persisting the payment fields a cashier typed into an invoice, so that switching to
    /// another open invoice and back does not lose them.
    /// </summary>
    /// <param name="PaymentMethod">The selected payment method.</param>
    /// <param name="AmountPaid">The amount typed into the paid box.</param>
    /// <param name="ReceiptCopies">The receipt copy count typed by the cashier.</param>
    /// <param name="SaleId">The invoice to save. The active invoice when omitted.</param>
    public sealed record SaveInvoiceDraftRequest(BookStore.Domain.Enums.PaymentMethod PaymentMethod, decimal AmountPaid, int ReceiptCopies = 1, Guid? SaleId = null);
}
