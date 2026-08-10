using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Queries.GetProducts;
using BookStore.Application.Features.Products.Queries.SearchProducts;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles product list queries.</summary>
public sealed class GetProductsHandler
{
    private readonly SearchProductsHandler _searchHandler;
    private readonly IValidator<GetProductsRequest> _validator;
    private readonly ILogger<GetProductsHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetProductsHandler"/> class.</summary>
    public GetProductsHandler(SearchProductsHandler searchHandler, IValidator<GetProductsRequest> validator, ILogger<GetProductsHandler> logger)
    {
        _searchHandler = searchHandler;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<ProductListItem>>> HandleAsync(GetProductsRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Product list validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<ProductListItem>>.Failure(validation.Errors[0].ErrorMessage);
        }

        return await _searchHandler.HandleAsync(new SearchProductsRequest(new ProductFilter { PageNumber = request.PageNumber, PageSize = request.PageSize }), cancellationToken);
    }
}
