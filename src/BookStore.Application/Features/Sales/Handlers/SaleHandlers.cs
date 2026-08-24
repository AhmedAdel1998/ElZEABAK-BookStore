using BookStore.Application.Features.Sales.Commands.AddItem;
using BookStore.Application.Features.Sales.Commands.ApplyInvoiceDiscount;
using BookStore.Application.Features.Sales.Commands.ApplyLineDiscount;
using BookStore.Application.Features.Sales.Commands.CancelSale;
using BookStore.Application.Features.Sales.Commands.CloseInvoice;
using BookStore.Application.Features.Sales.Commands.CompleteSale;
using BookStore.Application.Features.Sales.Commands.RemoveItem;
using BookStore.Application.Features.Sales.Commands.ResumeSale;
using BookStore.Application.Features.Sales.Commands.SaveInvoiceDraft;
using BookStore.Application.Features.Sales.Commands.ClearCustomer;
using BookStore.Application.Features.Sales.Commands.SelectCustomer;
using BookStore.Application.Features.Sales.Commands.StartSale;
using BookStore.Application.Features.Sales.Commands.SuspendSale;
using BookStore.Application.Features.Sales.Commands.SwitchInvoice;
using BookStore.Application.Features.Sales.Commands.UpdateItemQuantity;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Sales.Queries.GetCurrentSale;
using BookStore.Application.Features.Sales.Queries.GetHeldSales;
using BookStore.Application.Features.Sales.Queries.GetOpenInvoices;
using BookStore.Application.Features.Sales.Queries.GetSaleSummary;
using BookStore.Application.Features.Sales.Queries.SearchProduct;
using BookStore.Application.Features.Sales.Responses;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Sales.Handlers;

/// <summary>
/// Shared lookup used by every POS command that edits one of the invoices open on the workstation.
/// </summary>
internal static class PosSessionTarget
{
    /// <summary>The message used whenever a request names an invoice that is not open.</summary>
    internal const string NotOpen = "That invoice is not open.";

    /// <summary>The message used whenever a request needs an invoice and none is open.</summary>
    internal const string NoneOpen = "There is no open invoice.";

    /// <summary>
    /// Resolves the invoice a request is aimed at: the one it names, or the active one when it names
    /// none.
    /// </summary>
    internal static Task<SaleSessionDto?> ResolveAsync(IPosSaleSessionStore store, Guid? saleId, CancellationToken cancellationToken) =>
        saleId is null
            ? store.GetCurrentAsync(cancellationToken)
            : store.GetAsync(saleId.Value, cancellationToken);

    /// <summary>Picks the message that fits how the invoice was addressed.</summary>
    internal static string MissingMessage(Guid? saleId) => saleId is null ? NoneOpen : NotOpen;

    /// <summary>Stamps the last-edit time of an invoice.</summary>
    internal static SaleSessionDto Touch(SaleSessionDto sale)
    {
        sale.UpdatedAt = DateTimeOffset.UtcNow;
        return sale;
    }
}

/// <summary>Handles POS sale creation.</summary>
public sealed class StartSaleHandler
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPosCartStockService _cartStockService;
    private readonly IPricingService _pricingService;
    private readonly IValidator<StartSaleRequest> _validator;
    private readonly ILogger<StartSaleHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="StartSaleHandler"/> class.</summary>
    public StartSaleHandler(ICurrentUserService currentUserService, IPosSaleSessionStore sessionStore, IPosCartStockService cartStockService, IPricingService pricingService, IValidator<StartSaleRequest> validator, ILogger<StartSaleHandler> logger)
    {
        _currentUserService = currentUserService;
        _sessionStore = sessionStore;
        _cartStockService = cartStockService;
        _pricingService = pricingService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(StartSaleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SaleSessionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var active = await _sessionStore.GetCurrentAsync(cancellationToken);

        // Returning to the POS screen -- or restarting after a crash -- must land back on the invoice
        // the cashier was editing, however far along it is. Asking for a new invoice while sitting on
        // an untouched one reuses it rather than leaving an empty tab behind on every click.
        if (active is not null && (!request.ForceNew || IsBlank(active)))
        {
            await _cartStockService.SynchronizeAsync(active, cancellationToken);
            await _pricingService.RecalculateAsync(active, cancellationToken);
            await _sessionStore.SaveCurrentAsync(active, cancellationToken);
            _logger.LogInformation("POS invoice re-opened. Invoice={InvoiceNumber} Lines={Lines}", active.InvoiceNumber, active.Items.Count);
            return Result<SaleSessionDto>.Success(active);
        }

        // Invoices already open stay open. The limit exists so a strip of forgotten carts cannot hold
        // stock away from the invoices that are actually being served.
        var open = await _sessionStore.GetOpenAsync(cancellationToken);
        if (open.Count >= PosConstants.MaxOpenInvoices)
        {
            return Result<SaleSessionDto>.Failure($"No more than {PosConstants.MaxOpenInvoices} invoices can be open at once. Complete, hold or close one first.");
        }

        var sale = new SaleSessionDto
        {
            CashierId = _currentUserService.UserId ?? Guid.Empty,
            CashierName = _currentUserService.FullName ?? _currentUserService.Username ?? "Cashier",
            CustomerName = request.CustomerName.Trim()
        };

        // The timestamp alone collided when two sales were created in the same millisecond, which
        // the unique index on InvoiceNumber would later reject. The session id suffix makes it unique
        // by construction.
        var suffix = sale.SaleId.ToString("N")[..4].ToUpperInvariant();
        sale.InvoiceNumber = $"POS-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{suffix}";
        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveCurrentAsync(PosSessionTarget.Touch(sale), cancellationToken);
        _logger.LogInformation("POS invoice opened. Invoice={InvoiceNumber} Cashier={Cashier} OpenCount={OpenCount}", sale.InvoiceNumber, sale.CashierName, open.Count + 1);
        return Result<SaleSessionDto>.Success(sale);
    }

    /// <summary>An invoice nobody has typed anything into yet.</summary>
    private static bool IsBlank(SaleSessionDto sale) =>
        sale.Items.Count == 0 && sale.CustomerId is null && sale.InvoiceDiscount == 0m && sale.AmountPaid == 0m;
}

/// <summary>Handles adding products to a POS cart.</summary>
public sealed class AddItemHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPosCartStockService _cartStockService;
    private readonly IPricingService _pricingService;
    private readonly IValidator<AddItemRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="AddItemHandler"/> class.</summary>
    public AddItemHandler(IUnitOfWork unitOfWork, IPosSaleSessionStore sessionStore, IPosCartStockService cartStockService, IPricingService pricingService, IValidator<AddItemRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _sessionStore = sessionStore;
        _cartStockService = cartStockService;
        _pricingService = pricingService;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(AddItemRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SaleSessionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        // Fabricating a session here used to produce a cart with no invoice number, which then failed
        // at checkout with a domain validation error rather than at the till with a clear message.
        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure(PosSessionTarget.MissingMessage(request.SaleId));
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<SaleSessionDto>.Failure("Product was not found.");
        }

        if (!product.IsActive)
        {
            return Result<SaleSessionDto>.Failure("Inactive products cannot be sold.");
        }

        var existing = sale.Items.FirstOrDefault(item => item.ProductId == product.Id);
        var requestedQuantity = request.Quantity + (existing?.Quantity ?? 0);

        // Available means available to this invoice: whatever the other open invoices are holding is
        // already spoken for, so two tabs cannot both sell the last copy.
        var available = await _cartStockService.GetAvailableAsync(product.Id, sale.SaleId, cancellationToken);
        if (requestedQuantity > available)
        {
            return Result<SaleSessionDto>.Failure(available <= 0
                ? $"{product.Title} has no stock left for this invoice."
                : $"Requested quantity exceeds available stock. {available} left for this invoice.");
        }

        if (existing is null)
        {
            sale.Items.Add(new SaleCartItemDto
            {
                ProductId = product.Id,
                Barcode = product.Barcode.Value,
                Title = product.Title,
                CategoryName = product.Category?.Name,
                Quantity = request.Quantity,
                AvailableQuantity = available - request.Quantity,
                UnitPrice = product.SellingPrice
            });
        }
        else
        {
            existing.Quantity = requestedQuantity;
            existing.AvailableQuantity = available - requestedQuantity;
            existing.CategoryName = product.Category?.Name ?? existing.CategoryName;
            existing.UnitPrice = product.SellingPrice;
        }

        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveAsync(PosSessionTarget.Touch(sale), cancellationToken);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles cart quantity updates.</summary>
public sealed class UpdateItemQuantityHandler
{
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPosCartStockService _cartStockService;
    private readonly IPricingService _pricingService;
    private readonly IValidator<UpdateItemQuantityRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="UpdateItemQuantityHandler"/> class.</summary>
    public UpdateItemQuantityHandler(IPosSaleSessionStore sessionStore, IPosCartStockService cartStockService, IPricingService pricingService, IValidator<UpdateItemQuantityRequest> validator)
    {
        _sessionStore = sessionStore;
        _cartStockService = cartStockService;
        _pricingService = pricingService;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(UpdateItemQuantityRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SaleSessionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        var item = sale?.Items.FirstOrDefault(existing => existing.Id == request.ItemId);
        if (sale is null || item is null)
        {
            return Result<SaleSessionDto>.Failure("Cart item was not found.");
        }

        // The line's own cached AvailableQuantity was the only ceiling before, so a cart left open
        // while stock moved elsewhere could be raised past what the shelf actually held.
        var available = await _cartStockService.GetAvailableAsync(item.ProductId, sale.SaleId, cancellationToken);
        if (request.Quantity > available)
        {
            return Result<SaleSessionDto>.Failure(available <= 0
                ? $"{item.Title} has no stock left for this invoice."
                : $"Requested quantity exceeds available stock. {available} left for this invoice.");
        }

        item.Quantity = request.Quantity;
        item.AvailableQuantity = available - request.Quantity;
        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveAsync(PosSessionTarget.Touch(sale), cancellationToken);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles cart item removal.</summary>
public sealed class RemoveItemHandler
{
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;
    private readonly IValidator<RemoveItemRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="RemoveItemHandler"/> class.</summary>
    public RemoveItemHandler(IPosSaleSessionStore sessionStore, IPricingService pricingService, IValidator<RemoveItemRequest> validator)
    {
        _sessionStore = sessionStore;
        _pricingService = pricingService;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(RemoveItemRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SaleSessionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        var removed = sale?.Items.RemoveAll(item => item.Id == request.ItemId) > 0;
        if (sale is null || !removed)
        {
            return Result<SaleSessionDto>.Failure("Cart item was not found.");
        }

        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveAsync(PosSessionTarget.Touch(sale), cancellationToken);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles POS discount operations.</summary>
public sealed class ApplyLineDiscountHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;
    private readonly IValidator<ApplyLineDiscountRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="ApplyLineDiscountHandler"/> class.</summary>
    public ApplyLineDiscountHandler(IAuthorizationService authorizationService, IPosSaleSessionStore sessionStore, IPricingService pricingService, IValidator<ApplyLineDiscountRequest> validator)
    {
        _authorizationService = authorizationService;
        _sessionStore = sessionStore;
        _pricingService = pricingService;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(ApplyLineDiscountRequest request, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.SalesApplyDiscount))
        {
            return Result<SaleSessionDto>.Failure("Current user cannot apply discounts.");
        }

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SaleSessionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        var item = sale?.Items.FirstOrDefault(existing => existing.Id == request.ItemId);
        if (sale is null || item is null)
        {
            return Result<SaleSessionDto>.Failure("Cart item was not found.");
        }

        if (request.Discount > item.UnitPrice * item.Quantity)
        {
            return Result<SaleSessionDto>.Failure("Discount cannot exceed the line total.");
        }

        item.Discount = request.Discount;
        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveAsync(PosSessionTarget.Touch(sale), cancellationToken);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles POS invoice discount operations.</summary>
public sealed class ApplyInvoiceDiscountHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;
    private readonly IValidator<ApplyInvoiceDiscountRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="ApplyInvoiceDiscountHandler"/> class.</summary>
    public ApplyInvoiceDiscountHandler(IAuthorizationService authorizationService, IPosSaleSessionStore sessionStore, IPricingService pricingService, IValidator<ApplyInvoiceDiscountRequest> validator)
    {
        _authorizationService = authorizationService;
        _sessionStore = sessionStore;
        _pricingService = pricingService;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(ApplyInvoiceDiscountRequest request, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.SalesApplyDiscount))
        {
            return Result<SaleSessionDto>.Failure("Current user cannot apply discounts.");
        }

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SaleSessionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure(PosSessionTarget.MissingMessage(request.SaleId));
        }

        sale.InvoiceDiscount = request.Discount;
        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveAsync(PosSessionTarget.Touch(sale), cancellationToken);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles sale cancellation.</summary>
public sealed class CancelSaleHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IValidator<CancelSaleRequest> _validator;
    private readonly ILogger<CancelSaleHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="CancelSaleHandler"/> class.</summary>
    public CancelSaleHandler(IAuthorizationService authorizationService, IPosSaleSessionStore sessionStore, IValidator<CancelSaleRequest> validator, ILogger<CancelSaleHandler> logger)
    {
        _authorizationService = authorizationService;
        _sessionStore = sessionStore;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result> HandleAsync(CancelSaleRequest request, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.SalesCancel))
        {
            return Result.Failure("Current user cannot cancel sales.");
        }

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors[0].ErrorMessage);
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        if (sale is null)
        {
            // Reporting success for a cancellation that cancelled nothing hid genuine failures.
            return Result.Failure(request.SaleId is null ? "There is no open invoice to cancel." : PosSessionTarget.NotOpen);
        }

        await _sessionStore.CloseAsync(sale.SaleId, cancellationToken);
        _logger.LogWarning("POS invoice cancelled. Invoice={InvoiceNumber} Reason={Reason}", sale.InvoiceNumber, request.Reason);
        return Result.Success();
    }
}

/// <summary>Handles suspended POS sale creation.</summary>
public sealed class SuspendSaleHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IPosSaleSessionStore _sessionStore;

    /// <summary>Initializes a new instance of the <see cref="SuspendSaleHandler"/> class.</summary>
    public SuspendSaleHandler(IAuthorizationService authorizationService, IPosSaleSessionStore sessionStore)
    {
        _authorizationService = authorizationService;
        _sessionStore = sessionStore;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result> HandleAsync(SuspendSaleRequest request, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.SalesSuspend))
        {
            return Result.Failure("Current user cannot suspend sales.");
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        if (sale is null || sale.Items.Count == 0)
        {
            return Result.Failure("Only non-empty open invoices can be suspended.");
        }

        sale.IsSuspended = true;
        await _sessionStore.SuspendAsync(PosSessionTarget.Touch(sale), cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles suspended sale resume.</summary>
public sealed class ResumeSaleHandler
{
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPosCartStockService _cartStockService;
    private readonly IPricingService _pricingService;
    private readonly IValidator<ResumeSaleRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="ResumeSaleHandler"/> class.</summary>
    public ResumeSaleHandler(IPosSaleSessionStore sessionStore, IPosCartStockService cartStockService, IPricingService pricingService, IValidator<ResumeSaleRequest> validator)
    {
        _sessionStore = sessionStore;
        _cartStockService = cartStockService;
        _pricingService = pricingService;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(ResumeSaleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SaleSessionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);

        // Nothing is parked or overwritten any more: a resumed sale simply becomes another open
        // invoice next to the one already on screen.
        var open = await _sessionStore.GetOpenAsync(cancellationToken);
        if (open.Count >= PosConstants.MaxOpenInvoices)
        {
            return Result<SaleSessionDto>.Failure($"No more than {PosConstants.MaxOpenInvoices} invoices can be open at once. Complete, hold or close one first.");
        }

        var resumed = await _sessionStore.ResumeAsync(request.SaleId, cancellationToken);
        if (resumed is null)
        {
            return Result<SaleSessionDto>.Failure("Suspended sale was not found.");
        }

        resumed.IsSuspended = false;
        await _cartStockService.SynchronizeAsync(resumed, cancellationToken);
        await _pricingService.RecalculateAsync(resumed, cancellationToken);
        await _sessionStore.SaveCurrentAsync(PosSessionTarget.Touch(resumed), cancellationToken);
        return Result<SaleSessionDto>.Success(resumed);
    }
}

/// <summary>Handles switching the cashier screen between the invoices that are already open.</summary>
public sealed class SwitchInvoiceHandler
{
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPosCartStockService _cartStockService;
    private readonly IPricingService _pricingService;
    private readonly ILogger<SwitchInvoiceHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="SwitchInvoiceHandler"/> class.</summary>
    public SwitchInvoiceHandler(IPosSaleSessionStore sessionStore, IPosCartStockService cartStockService, IPricingService pricingService, ILogger<SwitchInvoiceHandler> logger)
    {
        _sessionStore = sessionStore;
        _cartStockService = cartStockService;
        _pricingService = pricingService;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(SwitchInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SaleId == Guid.Empty)
        {
            return Result<SaleSessionDto>.Failure(PosSessionTarget.NotOpen);
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await _sessionStore.SetActiveAsync(request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure(PosSessionTarget.NotOpen);
        }

        // A cart that has been sitting in the background is re-checked against live stock before the
        // cashier is allowed to keep building it.
        await _cartStockService.SynchronizeAsync(sale, cancellationToken);
        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
        _logger.LogInformation("POS switched invoice. Invoice={InvoiceNumber}", sale.InvoiceNumber);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles closing an open invoice without completing it.</summary>
public sealed class CloseInvoiceHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly ILogger<CloseInvoiceHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="CloseInvoiceHandler"/> class.</summary>
    public CloseInvoiceHandler(IAuthorizationService authorizationService, IPosSaleSessionStore sessionStore, ILogger<CloseInvoiceHandler> logger)
    {
        _authorizationService = authorizationService;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result> HandleAsync(CloseInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await _sessionStore.GetAsync(request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure(PosSessionTarget.NotOpen);
        }

        // Closing an empty invoice is housekeeping. Closing one with lines throws away a cart, which
        // is the same act as cancelling it and needs the same permission.
        if (sale.Items.Count > 0 && !_authorizationService.HasPermission(PermissionConstants.SalesCancel))
        {
            return Result.Failure("Current user cannot discard an invoice that has items.");
        }

        await _sessionStore.CloseAsync(request.SaleId, cancellationToken);
        _logger.LogInformation("POS invoice closed. Invoice={InvoiceNumber} Lines={Lines} Reason={Reason}", sale.InvoiceNumber, sale.Items.Count, request.Reason);
        return Result.Success();
    }
}

/// <summary>Handles persisting the payment fields typed into an open invoice.</summary>
public sealed class SaveInvoiceDraftHandler
{
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;

    /// <summary>Initializes a new instance of the <see cref="SaveInvoiceDraftHandler"/> class.</summary>
    public SaveInvoiceDraftHandler(IPosSaleSessionStore sessionStore, IPricingService pricingService)
    {
        _sessionStore = sessionStore;
        _pricingService = pricingService;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(SaveInvoiceDraftRequest request, CancellationToken cancellationToken = default)
    {
        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure(PosSessionTarget.MissingMessage(request.SaleId));
        }

        sale.PaymentMethod = request.PaymentMethod;
        sale.AmountPaid = Math.Max(request.AmountPaid, 0m);
        sale.ReceiptCopies = Math.Clamp(request.ReceiptCopies, 1, 5);
        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveAsync(sale, cancellationToken);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles POS sale completion.</summary>
public sealed class CompleteSaleHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;
    private readonly IReceiptService _receiptService;
    private readonly ICheckoutConcurrencyGuard _checkoutGuard;
    private readonly IValidator<CompleteSaleRequest> _validator;
    private readonly ILogger<CompleteSaleHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="CompleteSaleHandler"/> class.</summary>
    public CompleteSaleHandler(IAuthorizationService authorizationService, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, IPosSaleSessionStore sessionStore, IPricingService pricingService, IReceiptService receiptService, ICheckoutConcurrencyGuard checkoutGuard, IValidator<CompleteSaleRequest> validator, ILogger<CompleteSaleHandler> logger)
    {
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _sessionStore = sessionStore;
        _pricingService = pricingService;
        _receiptService = receiptService;
        _checkoutGuard = checkoutGuard;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<CompleteSaleResponse>> HandleAsync(CompleteSaleRequest request, CancellationToken cancellationToken = default)
    {
        // One checkout at a time for the whole workstation, however many invoices are open: the stock
        // rows they touch overlap, so overlapping commits are what oversells.
        if (!await _checkoutGuard.TryEnterAsync(cancellationToken))
        {
            return Result<CompleteSaleResponse>.Failure("Checkout is already in progress.");
        }

        try
        {
            return await CompleteAsync(request, cancellationToken);
        }
        finally
        {
            _checkoutGuard.Exit();
        }
    }

    private async Task<Result<CompleteSaleResponse>> CompleteAsync(CompleteSaleRequest request, CancellationToken cancellationToken)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.SalesComplete))
        {
            return Result<CompleteSaleResponse>.Failure("Current user cannot complete sales.");
        }

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<CompleteSaleResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        Guid completedSaleId;
        string completedInvoiceNumber;

        // The session lock is released before printing. It guards the invoice, and a printer that
        // takes its timeout to fail must not hold every other open invoice hostage meanwhile.
        await using (await _sessionStore.LockAsync(cancellationToken))
        {
            var session = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
            if (session is null || session.Items.Count == 0)
            {
                return Result<CompleteSaleResponse>.Failure("A sale must contain at least one item.");
            }

            session.PaymentMethod = request.PaymentMethod;
            session.AmountPaid = request.AmountPaid;
            session.ReceiptCopies = Math.Clamp(request.ReceiptCopies, 1, 5);
            await _pricingService.RecalculateAsync(session, cancellationToken);
            if (session.AmountPaid < session.Summary.GrandTotal)
            {
                return Result<CompleteSaleResponse>.Failure("Paid amount cannot be less than the total.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var userId = _currentUserService.UserId ?? session.CashierId;
                var sale = new Sale(session.InvoiceNumber, userId, request.PaymentMethod);
                sale.AssignCustomer(session.CustomerId);
                foreach (var cartItem in session.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId, cancellationToken);
                    if (product is null)
                    {
                        throw new InvalidOperationException($"Product {cartItem.ProductId} was not found.");
                    }

                    if (!product.IsActive)
                    {
                        throw new InvalidOperationException($"Product {product.Title} is inactive.");
                    }

                    if (product.Quantity < cartItem.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for {product.Title}.");
                    }

                    sale.AddItem(new SaleItem(product.Id, cartItem.Quantity, cartItem.UnitPrice, cartItem.Discount));

                    var quantityBefore = product.Quantity;
                    var quantityAfter = quantityBefore - cartItem.Quantity;
                    product.SetQuantity(quantityAfter);
                    await _unitOfWork.Inventory.AddAsync(
                        new InventoryTransaction(
                            product.Id,
                            -cartItem.Quantity,
                            InventoryTransactionType.Sale,
                            quantityBefore,
                            quantityAfter,
                            "POS sale",
                            session.InvoiceNumber,
                            userId,
                            _currentUserService.FullName ?? _currentUserService.Username ?? session.CashierName,
                            $"Completed sale {session.InvoiceNumber}"),
                        cancellationToken);
                }

                sale.UpdateCharges(session.InvoiceDiscount, session.Summary.Tax, session.Summary.TaxIncludedInPrice);
                sale.UpdatePaymentMethod(request.PaymentMethod);
                sale.Complete(session.AmountPaid);
                await _unitOfWork.Sales.AddAsync(sale, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Only the invoice that was paid for is closed. Every other open invoice stays exactly as
                // the cashier left it.
                await _sessionStore.CloseAsync(session.SaleId, cancellationToken);
                completedSaleId = sale.Id;
                completedInvoiceNumber = sale.InvoiceNumber;
                _logger.LogInformation("POS sale completed. Invoice={InvoiceNumber} Total={Total}", sale.InvoiceNumber, sale.Total);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "POS sale completion failed. Invoice={InvoiceNumber}", session.InvoiceNumber);
                return Result<CompleteSaleResponse>.Failure(ex.Message);
            }
        }

        ReceiptPrintResult printResult;
        try
        {
            printResult = await _receiptService.PrintCompletedSaleAsync(completedSaleId, PrintRequestKind.Automatic, copies: request.ReceiptCopies, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Receipt printing failed after sale commit. Invoice={InvoiceNumber}", completedInvoiceNumber);
            printResult = ReceiptPrintResult.Failure(
                Guid.NewGuid(),
                "Receipt printing failed unexpectedly.",
                receipt: new BookStore.Application.Features.Receipts.DTOs.ReceiptModel { SaleId = completedSaleId, InvoiceNumber = completedInvoiceNumber });
        }

        return Result<CompleteSaleResponse>.Success(new CompleteSaleResponse
        {
            SaleId = completedSaleId,
            InvoiceNumber = completedInvoiceNumber,
            Receipt = printResult.Receipt ?? new BookStore.Application.Features.Receipts.DTOs.ReceiptModel { SaleId = completedSaleId, InvoiceNumber = completedInvoiceNumber },
            ReceiptPrintSucceeded = printResult.Succeeded,
            ReceiptPrintError = printResult.Error,
            PrintRequestId = printResult.PrintRequestId,
            ReceiptCopies = request.ReceiptCopies
        });
    }
}

/// <summary>Handles selecting a customer for a POS invoice.</summary>
public sealed class SelectCustomerForSaleHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IValidator<SelectCustomerForSaleRequest> _validator;
    private readonly ILogger<SelectCustomerForSaleHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="SelectCustomerForSaleHandler"/> class.</summary>
    public SelectCustomerForSaleHandler(IUnitOfWork unitOfWork, IPosSaleSessionStore sessionStore, IValidator<SelectCustomerForSaleRequest> validator, ILogger<SelectCustomerForSaleHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _sessionStore = sessionStore;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(SelectCustomerForSaleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SaleSessionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            return Result<SaleSessionDto>.Failure("Customer could not be found.");
        }

        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure(PosSessionTarget.MissingMessage(request.SaleId));
        }

        sale.CustomerId = customer.Id;
        sale.CustomerName = customer.FullName;
        sale.CustomerPhone = customer.Phone?.Value;
        await _sessionStore.SaveAsync(PosSessionTarget.Touch(sale), cancellationToken);
        _logger.LogInformation("Customer selected during sale. CustomerId={CustomerId} Invoice={InvoiceNumber}", customer.Id, sale.InvoiceNumber);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles removing the selected customer from a POS invoice.</summary>
public sealed class ClearCustomerFromSaleHandler
{
    private readonly IPosSaleSessionStore _sessionStore;

    /// <summary>Initializes a new instance of the <see cref="ClearCustomerFromSaleHandler"/> class.</summary>
    public ClearCustomerFromSaleHandler(IPosSaleSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto>> HandleAsync(ClearCustomerFromSaleRequest request, CancellationToken cancellationToken = default)
    {
        await using var sessionLock = await _sessionStore.LockAsync(cancellationToken);
        var sale = await PosSessionTarget.ResolveAsync(_sessionStore, request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure(PosSessionTarget.MissingMessage(request.SaleId));
        }

        sale.CustomerId = null;
        sale.CustomerName = "Walk-in Customer";
        sale.CustomerPhone = null;
        await _sessionStore.SaveAsync(PosSessionTarget.Touch(sale), cancellationToken);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles POS product search.</summary>
public sealed class SearchProductHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SearchProductRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="SearchProductHandler"/> class.</summary>
    public SearchProductHandler(IUnitOfWork unitOfWork, IValidator<SearchProductRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<PosProductDto>>> HandleAsync(SearchProductRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<PosProductDto>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var products = await _unitOfWork.Products.SearchAsync(request.SearchTerm, true, false, null, null, null, null, null, request.PageNumber, request.PageSize, cancellationToken);
        var total = await _unitOfWork.Products.CountAsync(request.SearchTerm, true, false, null, null, null, null, null, cancellationToken);
        var items = products.Select(product => new PosProductDto
        {
            ProductId = product.Id,
            Barcode = product.Barcode.Value,
            ISBN = product.ISBN?.Value,
            Title = product.Title,
            CategoryName = product.Category?.Name,
            Author = product.Author,
            UnitPrice = product.SellingPrice,
            AvailableQuantity = product.Quantity,
            IsActive = product.IsActive
        }).ToArray();

        return Result<PagedResult<PosProductDto>>.Success(new PagedResult<PosProductDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total });
    }
}

/// <summary>Handles active POS sale lookup.</summary>
public sealed class GetCurrentSaleHandler
{
    private readonly IPosSaleSessionStore _sessionStore;

    /// <summary>Initializes a new instance of the <see cref="GetCurrentSaleHandler"/> class.</summary>
    public GetCurrentSaleHandler(IPosSaleSessionStore sessionStore) => _sessionStore = sessionStore;

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSessionDto?>> HandleAsync(GetCurrentSaleRequest request, CancellationToken cancellationToken = default) => Result<SaleSessionDto?>.Success(await _sessionStore.GetCurrentAsync(cancellationToken));
}

/// <summary>Handles open POS invoice lookup.</summary>
public sealed class GetOpenInvoicesHandler
{
    private readonly IPosSaleSessionStore _sessionStore;

    /// <summary>Initializes a new instance of the <see cref="GetOpenInvoicesHandler"/> class.</summary>
    public GetOpenInvoicesHandler(IPosSaleSessionStore sessionStore) => _sessionStore = sessionStore;

    /// <summary>Handles the request.</summary>
    public async Task<Result<IReadOnlyCollection<SaleSessionDto>>> HandleAsync(GetOpenInvoicesRequest request, CancellationToken cancellationToken = default) => Result<IReadOnlyCollection<SaleSessionDto>>.Success(await _sessionStore.GetOpenAsync(cancellationToken));
}

/// <summary>Handles suspended POS sale lookup.</summary>
public sealed class GetHeldSalesHandler
{
    private readonly IPosSaleSessionStore _sessionStore;

    /// <summary>Initializes a new instance of the <see cref="GetHeldSalesHandler"/> class.</summary>
    public GetHeldSalesHandler(IPosSaleSessionStore sessionStore) => _sessionStore = sessionStore;

    /// <summary>Handles the request.</summary>
    public async Task<Result<IReadOnlyCollection<SaleSessionDto>>> HandleAsync(GetHeldSalesRequest request, CancellationToken cancellationToken = default) => Result<IReadOnlyCollection<SaleSessionDto>>.Success(await _sessionStore.GetHeldAsync(cancellationToken));
}

/// <summary>Handles active POS sale summary lookup.</summary>
public sealed class GetSaleSummaryHandler
{
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;

    /// <summary>Initializes a new instance of the <see cref="GetSaleSummaryHandler"/> class.</summary>
    public GetSaleSummaryHandler(IPosSaleSessionStore sessionStore, IPricingService pricingService)
    {
        _sessionStore = sessionStore;
        _pricingService = pricingService;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SaleSummaryDto>> HandleAsync(GetSaleSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var sale = await _sessionStore.GetCurrentAsync(cancellationToken);
        return Result<SaleSummaryDto>.Success(sale is null ? new SaleSummaryDto() : await _pricingService.RecalculateAsync(sale, cancellationToken));
    }
}
