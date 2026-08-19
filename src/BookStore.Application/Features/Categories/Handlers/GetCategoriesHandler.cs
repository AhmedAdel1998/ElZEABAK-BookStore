using AutoMapper;
using BookStore.Application.Features.Categories.DTOs;
using BookStore.Application.Features.Categories.Queries.GetCategories;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Categories.Handlers;

/// <summary>
/// Handles category list queries.
/// </summary>
public sealed class GetCategoriesHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetCategoriesRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCategoriesHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCategoriesHandler"/> class.
    /// </summary>
    public GetCategoriesHandler(IUnitOfWork unitOfWork, IValidator<GetCategoriesRequest> validator, IMapper mapper, ILogger<GetCategoriesHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    public async Task<Result<PagedResult<CategoryDto>>> HandleAsync(GetCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Category list validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<CategoryDto>>.Failure(validation.Errors[0].ErrorMessage);
        }

        return await LoadAsync(null, request.PageNumber, request.PageSize, isActive: null, cancellationToken);
    }

    internal async Task<Result<PagedResult<CategoryDto>>> LoadAsync(string? searchTerm, int pageNumber, int pageSize, bool? isActive, CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.SearchAsync(searchTerm, pageNumber, pageSize, isActive, cancellationToken);
        var totalCount = await _unitOfWork.Categories.CountAsync(searchTerm, cancellationToken);
        var counts = await _unitOfWork.Categories.GetProductCountsAsync(categories.Select(category => category.Id), cancellationToken);
        var items = categories.Select(category =>
        {
            var dto = _mapper.Map<CategoryDto>(category);
            dto.ProductCount = counts.TryGetValue(category.Id, out var count) ? count : 0;
            return dto;
        }).ToArray();

        return Result<PagedResult<CategoryDto>>.Success(new PagedResult<CategoryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }
}
