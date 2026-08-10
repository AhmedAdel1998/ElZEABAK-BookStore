using BookStore.Application.Features.Categories.Commands.DeleteCategory;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Categories.Handlers;

/// <summary>
/// Handles category soft deletion.
/// </summary>
public sealed class DeleteCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeleteCategoryRequest> _validator;
    private readonly ILogger<DeleteCategoryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCategoryHandler"/> class.
    /// </summary>
    public DeleteCategoryHandler(IUnitOfWork unitOfWork, IValidator<DeleteCategoryRequest> validator, ILogger<DeleteCategoryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    public async Task<OperationResult> HandleAsync(DeleteCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Category delete validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return OperationResult.Failure(new Error("Category.NotFound", "Category was not found."));
        }

        var productCount = await _unitOfWork.Categories.GetProductCountAsync(request.Id, cancellationToken);
        if (productCount > 0)
        {
            _logger.LogWarning("Category delete blocked because products are assigned: {CategoryId}", request.Id);
            return OperationResult.Failure(new Error("Category.HasProducts", "Cannot delete a category that contains products."));
        }

        try
        {
            category.MarkDeleted();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category deleted: {CategoryId} {CategoryName}", category.Id, category.Name);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while deleting category {CategoryId}", request.Id);
            return OperationResult.Failure(new Error("Category.DeleteFailed", "Unable to delete category."));
        }
    }
}
