using BookStore.Application.Features.Sales.Commands.AddItem;
using BookStore.Application.Features.Sales.Commands.ApplyInvoiceDiscount;
using BookStore.Application.Features.Sales.Commands.ApplyLineDiscount;
using BookStore.Application.Features.Sales.Commands.CancelSale;
using BookStore.Application.Features.Sales.Commands.CompleteSale;
using BookStore.Application.Features.Sales.Commands.RemoveItem;
using BookStore.Application.Features.Sales.Commands.ResumeSale;
using BookStore.Application.Features.Sales.Commands.ClearCustomer;
using BookStore.Application.Features.Sales.Commands.SelectCustomer;
using BookStore.Application.Features.Sales.Commands.StartSale;
using BookStore.Application.Features.Sales.Commands.SuspendSale;
using BookStore.Application.Features.Sales.Commands.UpdateItemQuantity;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Sales.Queries.GetCurrentSale;
using BookStore.Application.Features.Sales.Queries.GetHeldSales;
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

/// <summary>Handles POS sale creation.</summary>
public sealed class StartSaleHandler
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;
    private readonly IValidator<StartSaleRequest> _validator;
    private readonly ILogger<StartSaleHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="StartSaleHandler"/> class.</summary>
    public StartSaleHandler(ICurrentUserService currentUserService, IPosSaleSessionStore sessionStore, IPricingService pricingService, IValidator<StartSaleRequest> validator, ILogger<StartSaleHandler> logger)
    {
        _currentUserService = currentUserService;
        _sessionStore = sessionStore;
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

        var existing = await _sessionStore.GetCurrentAsync(cancellationToken);
        if (existing is { IsSuspended: false, Items.Count: > 0 })
        {
            if (!request.ForceNew)
            {
                // Resume rather than replace. Returning to the POS screen -- or restarting after a
                // crash -- must not discard a cart that already has items in it.
                await _pricingService.RecalculateAsync(existing, cancellationToken);
                _logger.LogInformation("POS sale resumed in progress. Invoice={InvoiceNumber} Lines={Lines}", existing.InvoiceNumber, existing.Items.Count);
                return Result<SaleSessionDto>.Success(existing);
            }

            // An explicit new sale parks the current cart in Held Sales, so nothing is lost.
            existing.IsSuspended = true;
            await _sessionStore.SuspendAsync(existing, cancellationToken);
            _logger.LogInformation("Active POS cart suspended to start a new sale. Invoice={InvoiceNumber}", existing.InvoiceNumber);
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
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
        _logger.LogInformation("POS sale started. Invoice={InvoiceNumber} Cashier={Cashier}", sale.InvoiceNumber, sale.CashierName);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles adding products to the active POS cart.</summary>
public sealed class AddItemHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;
    private readonly IValidator<AddItemRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="AddItemHandler"/> class.</summary>
    public AddItemHandler(IUnitOfWork unitOfWork, IPosSaleSessionStore sessionStore, IPricingService pricingService, IValidator<AddItemRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _sessionStore = sessionStore;
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

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<SaleSessionDto>.Failure("Product was not found.");
        }

        if (!product.IsActive)
        {
            return Result<SaleSessionDto>.Failure("Inactive products cannot be sold.");
        }

        var sale = await _sessionStore.GetCurrentAsync(cancellationToken) ?? new SaleSessionDto();
        var existing = sale.Items.FirstOrDefault(item => item.ProductId == product.Id);
        var requestedQuantity = request.Quantity + (existing?.Quantity ?? 0);
        if (requestedQuantity > product.Quantity)
        {
            return Result<SaleSessionDto>.Failure("Requested quantity exceeds available stock.");
        }

        if (existing is null)
        {
            sale.Items.Add(ToCartItem(product, request.Quantity));
        }
        else
        {
            existing.Quantity = requestedQuantity;
            existing.AvailableQuantity = product.Quantity - requestedQuantity;
            existing.CategoryName = product.Category?.Name;
        }

        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
        return Result<SaleSessionDto>.Success(sale);
    }

    private static SaleCartItemDto ToCartItem(Product product, int quantity) => new()
    {
        ProductId = product.Id,
        Barcode = product.Barcode.Value,
        Title = product.Title,
        CategoryName = product.Category?.Name,
        Quantity = quantity,
        AvailableQuantity = product.Quantity - quantity,
        UnitPrice = product.SellingPrice
    };
}

/// <summary>Handles cart quantity updates.</summary>
public sealed class UpdateItemQuantityHandler
{
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IPricingService _pricingService;
    private readonly IValidator<UpdateItemQuantityRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="UpdateItemQuantityHandler"/> class.</summary>
    public UpdateItemQuantityHandler(IPosSaleSessionStore sessionStore, IPricingService pricingService, IValidator<UpdateItemQuantityRequest> validator)
    {
        _sessionStore = sessionStore;
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

        var sale = await _sessionStore.GetCurrentAsync(cancellationToken);
        var item = sale?.Items.FirstOrDefault(existing => existing.Id == request.ItemId);
        if (sale is null || item is null)
        {
            return Result<SaleSessionDto>.Failure("Cart item was not found.");
        }

        var maximumQuantity = item.Quantity + item.AvailableQuantity;
        if (request.Quantity > maximumQuantity)
        {
            return Result<SaleSessionDto>.Failure("Requested quantity exceeds available stock.");
        }

        item.Quantity = request.Quantity;
        item.AvailableQuantity = maximumQuantity - request.Quantity;
        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
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

        var sale = await _sessionStore.GetCurrentAsync(cancellationToken);
        var removed = sale?.Items.RemoveAll(item => item.Id == request.ItemId) > 0;
        if (sale is null || !removed)
        {
            return Result<SaleSessionDto>.Failure("Cart item was not found.");
        }

        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
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

        var sale = await _sessionStore.GetCurrentAsync(cancellationToken);
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
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
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

        var sale = await _sessionStore.GetCurrentAsync(cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure("There is no active sale.");
        }

        sale.InvoiceDiscount = request.Discount;
        await _pricingService.RecalculateAsync(sale, cancellationToken);
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
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

        var sale = await _sessionStore.GetCurrentAsync(cancellationToken);
        await _sessionStore.ClearCurrentAsync(cancellationToken);
        _logger.LogWarning("POS sale cancelled. Invoice={InvoiceNumber} Reason={Reason}", sale?.InvoiceNumber, request.Reason);
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

        var sale = await _sessionStore.GetCurrentAsync(cancellationToken);
        if (sale is null || sale.Items.Count == 0)
        {
            return Result.Failure("Only non-empty active sales can be suspended.");
        }

        sale.IsSuspended = true;
        await _sessionStore.SuspendAsync(sale, cancellationToken);
        await _sessionStore.ClearCurrentAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles suspended sale resume.</summary>
public sealed class ResumeSaleHandler
{
    private readonly IPosSaleSessionStore _sessionStore;
    private readonly IValidator<ResumeSaleRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="ResumeSaleHandler"/> class.</summary>
    public ResumeSaleHandler(IPosSaleSessionStore sessionStore, IValidator<ResumeSaleRequest> validator)
    {
        _sessionStore = sessionStore;
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

        // Whatever is on screen is parked, not binned. Resuming used to overwrite the active cart
        // outright, so a cashier halfway through an order lost it with no warning and no way back.
        var active = await _sessionStore.GetCurrentAsync(cancellationToken);
        if (active is { Items.Count: > 0 } && active.SaleId != request.SaleId)
        {
            active.IsSuspended = true;
            await _sessionStore.SuspendAsync(active, cancellationToken);
        }

        var sale = await _sessionStore.ResumeAsync(request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure("Suspended sale was not found.");
        }

        sale.IsSuspended = false;
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
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

        var session = await _sessionStore.GetCurrentAsync(cancellationToken);
        if (session is null || session.Items.Count == 0)
        {
            return Result<CompleteSaleResponse>.Failure("A sale must contain at least one item.");
        }

        session.PaymentMethod = request.PaymentMethod;
        session.AmountPaid = request.AmountPaid;
        await _pricingService.RecalculateAsync(session, cancellationToken);
        if (session.AmountPaid < session.Summary.GrandTotal)
        {
            return Result<CompleteSaleResponse>.Failure("Paid amount cannot be less than the total.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        Guid completedSaleId;
        string completedInvoiceNumber;
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
            await _sessionStore.ClearCurrentAsync(cancellationToken);
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
                "Sale completed, but receipt printing failed.",
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

/// <summary>Handles selecting a customer for the active POS sale.</summary>
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

        var sale = await _sessionStore.GetCurrentAsync(cancellationToken) ?? new SaleSessionDto();
        sale.CustomerId = customer.Id;
        sale.CustomerName = customer.FullName;
        sale.CustomerPhone = customer.Phone?.Value;
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
        _logger.LogInformation("Customer selected during sale. CustomerId={CustomerId} Invoice={InvoiceNumber}", customer.Id, sale.InvoiceNumber);
        return Result<SaleSessionDto>.Success(sale);
    }
}

/// <summary>Handles removing the selected customer from the active POS sale.</summary>
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
        var sale = await _sessionStore.GetCurrentAsync(cancellationToken);
        if (sale is null)
        {
            return Result<SaleSessionDto>.Failure("There is no active sale.");
        }

        sale.CustomerId = null;
        sale.CustomerName = "Walk-in Customer";
        sale.CustomerPhone = null;
        await _sessionStore.SaveCurrentAsync(sale, cancellationToken);
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
