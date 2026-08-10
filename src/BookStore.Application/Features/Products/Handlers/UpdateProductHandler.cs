using AutoMapper;
using BookStore.Application.Features.Products.Commands.UpdateProduct;
using BookStore.Application.Features.Products.Responses;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles product updates.</summary>
public sealed class UpdateProductHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateProductRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateProductHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="UpdateProductHandler"/> class.</summary>
    public UpdateProductHandler(IUnitOfWork unitOfWork, IValidator<UpdateProductRequest> validator, IMapper mapper, ILogger<UpdateProductHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<ProductResponse>> HandleAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Product update validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<ProductResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.Product.Id!.Value, cancellationToken);
        if (product is null)
        {
            return Result<ProductResponse>.Failure("Product was not found.");
        }

        try
        {
            ProductHandlerHelpers.ApplyEditor(product, request.Product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Product updated: {ProductId} {Title}", product.Id, product.Title);
            return Result<ProductResponse>.Success(ProductHandlerHelpers.MapResponse(_mapper, product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while updating product {ProductId}", request.Product.Id);
            return Result<ProductResponse>.Failure("Unable to update product.");
        }
    }
}
