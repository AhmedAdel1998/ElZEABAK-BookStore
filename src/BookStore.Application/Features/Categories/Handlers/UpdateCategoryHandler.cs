using AutoMapper;
using BookStore.Application.Features.Categories.Commands.UpdateCategory;
using BookStore.Application.Features.Categories.Responses;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Categories.Handlers;

/// <summary>
/// Handles category updates.
/// </summary>
public sealed class UpdateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateCategoryRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateCategoryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryHandler"/> class.
    /// </summary>
    public UpdateCategoryHandler(IUnitOfWork unitOfWork, IValidator<UpdateCategoryRequest> validator, IMapper mapper, ILogger<UpdateCategoryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    public async Task<Result<CategoryResponse>> HandleAsync(UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Category update validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<CategoryResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result<CategoryResponse>.Failure("Category was not found.");
        }

        try
        {
            category.Rename(request.Name);
            category.UpdateDescription(request.Description);
            if (request.IsActive)
            {
                category.Activate();
            }
            else
            {
                category.Deactivate();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var response = _mapper.Map<CategoryResponse>(category);
            response.ProductCount = await _unitOfWork.Categories.GetProductCountAsync(category.Id, cancellationToken);
            _logger.LogInformation("Category updated: {CategoryId} {CategoryName}", category.Id, category.Name);
            return Result<CategoryResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while updating category {CategoryId}", request.Id);
            return Result<CategoryResponse>.Failure("Unable to update category.");
        }
    }
}
