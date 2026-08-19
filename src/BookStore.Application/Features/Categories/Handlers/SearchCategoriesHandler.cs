using BookStore.Application.Features.Categories.DTOs;
using BookStore.Application.Features.Categories.Queries.SearchCategories;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Categories.Handlers;

/// <summary>
/// Handles category search queries.
/// </summary>
public sealed class SearchCategoriesHandler
{
    private readonly GetCategoriesHandler _getCategoriesHandler;
    private readonly IValidator<SearchCategoriesRequest> _validator;
    private readonly ILogger<SearchCategoriesHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchCategoriesHandler"/> class.
    /// </summary>
    public SearchCategoriesHandler(GetCategoriesHandler getCategoriesHandler, IValidator<SearchCategoriesRequest> validator, ILogger<SearchCategoriesHandler> logger)
    {
        _getCategoriesHandler = getCategoriesHandler;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    public async Task<Result<PagedResult<CategoryDto>>> HandleAsync(SearchCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Category search validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<CategoryDto>>.Failure(validation.Errors[0].ErrorMessage);
        }

        return await _getCategoriesHandler.LoadAsync(request.SearchTerm, request.PageNumber, request.PageSize, request.IsActive, cancellationToken);
    }
}
