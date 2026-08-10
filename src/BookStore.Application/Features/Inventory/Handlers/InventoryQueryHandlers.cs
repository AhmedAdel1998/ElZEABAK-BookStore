using AutoMapper;
using BookStore.Application.Features.Inventory.DTOs;
using BookStore.Application.Features.Inventory.Queries.GetInventory;
using BookStore.Application.Features.Inventory.Queries.GetInventoryDashboard;
using BookStore.Application.Features.Inventory.Queries.GetInventoryHistory;
using BookStore.Application.Features.Inventory.Queries.GetLowStockProducts;
using BookStore.Application.Features.Inventory.Queries.GetOutOfStockProducts;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Inventory.Handlers;

/// <summary>Handles inventory list queries.</summary>
public sealed class GetInventoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetInventoryRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<GetInventoryHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetInventoryHandler"/> class.</summary>
    public GetInventoryHandler(IUnitOfWork unitOfWork, IValidator<GetInventoryRequest> validator, IMapper mapper, ILogger<GetInventoryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<InventoryItemDto>>> HandleAsync(GetInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Inventory list validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<InventoryItemDto>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var minQuantity = request.OutOfStockOnly ? 0 : (int?)null;
        var maxQuantity = request.OutOfStockOnly ? 0 : (int?)null;
        var products = await _unitOfWork.Products.SearchAsync(request.SearchTerm, true, request.LowStockOnly, request.CategoryId, null, null, minQuantity, maxQuantity, request.PageNumber, request.PageSize, cancellationToken);
        var count = await _unitOfWork.Products.CountAsync(request.SearchTerm, true, request.LowStockOnly, request.CategoryId, null, null, minQuantity, maxQuantity, cancellationToken);

        return Result<PagedResult<InventoryItemDto>>.Success(new PagedResult<InventoryItemDto>
        {
            Items = products.Select(_mapper.Map<InventoryItemDto>).ToArray(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = count
        });
    }
}

/// <summary>Handles inventory history queries.</summary>
public sealed class GetInventoryHistoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetInventoryHistoryRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<GetInventoryHistoryHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetInventoryHistoryHandler"/> class.</summary>
    public GetInventoryHistoryHandler(IUnitOfWork unitOfWork, IValidator<GetInventoryHistoryRequest> validator, IMapper mapper, ILogger<GetInventoryHistoryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<InventoryTransactionDto>>> HandleAsync(GetInventoryHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Inventory history validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<InventoryTransactionDto>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var transactions = await _unitOfWork.Inventory.SearchHistoryAsync(request.ProductId, request.DateFrom, request.DateTo, request.TransactionType, request.UserId, request.PageNumber, request.PageSize, cancellationToken);
        var count = await _unitOfWork.Inventory.CountHistoryAsync(request.ProductId, request.DateFrom, request.DateTo, request.TransactionType, request.UserId, cancellationToken);
        var productIds = transactions.Select(transaction => transaction.ProductId).Distinct().ToArray();
        var products = await _unitOfWork.Products.ListAsync(null, cancellationToken);
        var productNames = products.Where(product => productIds.Contains(product.Id)).ToDictionary(product => product.Id, product => product.Title);

        var items = transactions.Select(transaction =>
        {
            var dto = _mapper.Map<InventoryTransactionDto>(transaction);
            dto.ProductTitle = productNames.TryGetValue(transaction.ProductId, out var title) ? title : transaction.ProductId.ToString();
            return dto;
        }).ToArray();

        return Result<PagedResult<InventoryTransactionDto>>.Success(new PagedResult<InventoryTransactionDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = count
        });
    }
}

/// <summary>Handles low-stock inventory queries.</summary>
public sealed class GetLowStockProductsHandler
{
    private readonly GetInventoryHandler _inventoryHandler;
    private readonly IValidator<GetLowStockProductsRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="GetLowStockProductsHandler"/> class.</summary>
    public GetLowStockProductsHandler(GetInventoryHandler inventoryHandler, IValidator<GetLowStockProductsRequest> validator)
    {
        _inventoryHandler = inventoryHandler;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<InventoryItemDto>>> HandleAsync(GetLowStockProductsRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        return !validation.IsValid
            ? Result<PagedResult<InventoryItemDto>>.Failure(validation.Errors[0].ErrorMessage)
            : await _inventoryHandler.HandleAsync(new GetInventoryRequest(LowStockOnly: true, PageNumber: request.PageNumber, PageSize: request.PageSize), cancellationToken);
    }
}

/// <summary>Handles out-of-stock inventory queries.</summary>
public sealed class GetOutOfStockProductsHandler
{
    private readonly GetInventoryHandler _inventoryHandler;
    private readonly IValidator<GetOutOfStockProductsRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="GetOutOfStockProductsHandler"/> class.</summary>
    public GetOutOfStockProductsHandler(GetInventoryHandler inventoryHandler, IValidator<GetOutOfStockProductsRequest> validator)
    {
        _inventoryHandler = inventoryHandler;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<InventoryItemDto>>> HandleAsync(GetOutOfStockProductsRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        return !validation.IsValid
            ? Result<PagedResult<InventoryItemDto>>.Failure(validation.Errors[0].ErrorMessage)
            : await _inventoryHandler.HandleAsync(new GetInventoryRequest(OutOfStockOnly: true, PageNumber: request.PageNumber, PageSize: request.PageSize), cancellationToken);
    }
}

/// <summary>Handles inventory dashboard queries.</summary>
public sealed class GetInventoryDashboardHandler
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="GetInventoryDashboardHandler"/> class.</summary>
    public GetInventoryDashboardHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<InventoryDashboardDto>> HandleAsync(GetInventoryDashboardRequest request, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Products.SearchAsync(null, true, false, null, null, null, null, null, 1, 100000, cancellationToken);
        var items = products.ToArray();
        return Result<InventoryDashboardDto>.Success(new InventoryDashboardDto
        {
            TotalProducts = items.Length,
            TotalStock = items.Sum(product => product.Quantity),
            LowStockCount = items.Count(product => product.Quantity <= product.MinimumStock && product.Quantity > 0),
            OutOfStockCount = items.Count(product => product.Quantity == 0),
            InventoryValue = items.Sum(product => product.Quantity * product.PurchasePrice),
            TotalPurchaseValue = items.Sum(product => product.Quantity * product.PurchasePrice),
            TotalSellingValue = items.Sum(product => product.Quantity * product.SellingPrice)
        });
    }
}
