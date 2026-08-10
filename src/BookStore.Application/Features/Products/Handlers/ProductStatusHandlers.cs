using BookStore.Application.Features.Products.Commands.ActivateProduct;
using BookStore.Application.Features.Products.Commands.DeactivateProduct;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles product activation.</summary>
public sealed class ActivateProductHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ActivateProductRequest> _validator;
    private readonly ILogger<ActivateProductHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ActivateProductHandler"/> class.</summary>
    public ActivateProductHandler(IUnitOfWork unitOfWork, IValidator<ActivateProductRequest> validator, ILogger<ActivateProductHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(ActivateProductRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return OperationResult.Failure(new Error("Product.NotFound", "Product was not found."));
        }

        product.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product activated: {ProductId}", product.Id);
        return OperationResult.Success();
    }
}

/// <summary>Handles product deactivation.</summary>
public sealed class DeactivateProductHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeactivateProductRequest> _validator;
    private readonly ILogger<DeactivateProductHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeactivateProductHandler"/> class.</summary>
    public DeactivateProductHandler(IUnitOfWork unitOfWork, IValidator<DeactivateProductRequest> validator, ILogger<DeactivateProductHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(DeactivateProductRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return OperationResult.Failure(new Error("Product.NotFound", "Product was not found."));
        }

        product.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product deactivated: {ProductId}", product.Id);
        return OperationResult.Success();
    }
}
