using AutoMapper;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Queries.SearchProducts;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles product search queries.</summary>
public sealed class SearchProductsHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SearchProductsRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchProductsHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="SearchProductsHandler"/> class.</summary>
    public SearchProductsHandler(IUnitOfWork unitOfWork, IValidator<SearchProductsRequest> validator, IMapper mapper, ILogger<SearchProductsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<ProductListItem>>> HandleAsync(SearchProductsRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Product search validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<ProductListItem>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var filter = request.Filter;
        var products = await _unitOfWork.Products.SearchAsync(filter.SearchTerm, filter.IsActive, filter.LowStockOnly, filter.CategoryId, filter.MinPrice, filter.MaxPrice, filter.MinQuantity, filter.MaxQuantity, filter.PageNumber, filter.PageSize, cancellationToken);
        var count = await _unitOfWork.Products.CountAsync(filter.SearchTerm, filter.IsActive, filter.LowStockOnly, filter.CategoryId, filter.MinPrice, filter.MaxPrice, filter.MinQuantity, filter.MaxQuantity, cancellationToken);

        return Result<PagedResult<ProductListItem>>.Success(new PagedResult<ProductListItem>
        {
            Items = products.Select(_mapper.Map<ProductListItem>).ToArray(),
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = count
        });
    }
}
