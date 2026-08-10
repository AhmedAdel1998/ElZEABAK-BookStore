using AutoMapper;
using BookStore.Application.Features.Categories.DTOs;
using BookStore.Application.Features.Categories.Queries.GetCategoryById;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Categories.Handlers;

/// <summary>
/// Handles category details queries.
/// </summary>
public sealed class GetCategoryByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetCategoryByIdRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCategoryByIdHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCategoryByIdHandler"/> class.
    /// </summary>
    public GetCategoryByIdHandler(IUnitOfWork unitOfWork, IValidator<GetCategoryByIdRequest> validator, IMapper mapper, ILogger<GetCategoryByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    public async Task<Result<CategoryDto>> HandleAsync(GetCategoryByIdRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Category details validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<CategoryDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result<CategoryDto>.Failure("Category was not found.");
        }

        var dto = _mapper.Map<CategoryDto>(category);
        dto.ProductCount = await _unitOfWork.Categories.GetProductCountAsync(category.Id, cancellationToken);
        return Result<CategoryDto>.Success(dto);
    }
}
