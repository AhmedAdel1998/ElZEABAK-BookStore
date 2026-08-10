using BookStore.Application.Features.Categories.Commands.DeactivateCategory;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Categories.Handlers;

/// <summary>
/// Handles category deactivation.
/// </summary>
public sealed class DeactivateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeactivateCategoryRequest> _validator;
    private readonly ILogger<DeactivateCategoryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeactivateCategoryHandler"/> class.
    /// </summary>
    public DeactivateCategoryHandler(IUnitOfWork unitOfWork, IValidator<DeactivateCategoryRequest> validator, ILogger<DeactivateCategoryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    public async Task<OperationResult> HandleAsync(DeactivateCategoryRequest request, CancellationToken cancellationToken = default)
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

        category.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category deactivated: {CategoryId}", request.Id);
        return OperationResult.Success();
    }
}
