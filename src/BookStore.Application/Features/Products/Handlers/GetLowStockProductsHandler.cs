using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Queries.GetLowStockProducts;
using BookStore.Application.Features.Products.Queries.SearchProducts;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles low-stock product queries.</summary>
public sealed class GetLowStockProductsHandler
{
    private readonly SearchProductsHandler _searchHandler;
    private readonly IValidator<GetLowStockProductsRequest> _validator;
    private readonly ILogger<GetLowStockProductsHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetLowStockProductsHandler"/> class.</summary>
    public GetLowStockProductsHandler(SearchProductsHandler searchHandler, IValidator<GetLowStockProductsRequest> validator, ILogger<GetLowStockProductsHandler> logger)
    {
        _searchHandler = searchHandler;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<ProductListItem>>> HandleAsync(GetLowStockProductsRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Low-stock product query validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<ProductListItem>>.Failure(validation.Errors[0].ErrorMessage);
        }

        return await _searchHandler.HandleAsync(new SearchProductsRequest(new ProductFilter { LowStockOnly = true, PageNumber = request.PageNumber, PageSize = request.PageSize }), cancellationToken);
    }
}
