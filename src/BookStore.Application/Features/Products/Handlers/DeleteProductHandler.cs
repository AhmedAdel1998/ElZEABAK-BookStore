using BookStore.Application.Features.Products.Commands.DeleteProduct;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles product soft deletion.</summary>
public sealed class DeleteProductHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeleteProductRequest> _validator;
    private readonly ILogger<DeleteProductHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteProductHandler"/> class.</summary>
    public DeleteProductHandler(IUnitOfWork unitOfWork, IValidator<DeleteProductRequest> validator, ILogger<DeleteProductHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(DeleteProductRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Product delete validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return OperationResult.Failure(new Error("Product.NotFound", "Product was not found."));
        }

        if (await _unitOfWork.Products.HasCompletedSaleReferencesAsync(request.Id, cancellationToken))
        {
            return OperationResult.Failure(new Error("Product.CompletedSaleReference", "Cannot delete a product referenced by completed sales."));
        }

        product.MarkDeleted();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product deleted: {ProductId} {Title}", product.Id, product.Title);
        return OperationResult.Success();
    }
}
