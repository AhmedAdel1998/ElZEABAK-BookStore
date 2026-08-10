using BookStore.Application.Features.Categories.Commands.ActivateCategory;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Categories.Handlers;

/// <summary>
/// Handles category activation.
/// </summary>
public sealed class ActivateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ActivateCategoryRequest> _validator;
    private readonly ILogger<ActivateCategoryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivateCategoryHandler"/> class.
    /// </summary>
    public ActivateCategoryHandler(IUnitOfWork unitOfWork, IValidator<ActivateCategoryRequest> validator, ILogger<ActivateCategoryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    public async Task<OperationResult> HandleAsync(ActivateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return OperationResult.Failure(new Error("Category.NotFound", "Category was not found."));
        }

        category.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category activated: {CategoryId}", request.Id);
        return OperationResult.Success();
    }
}
