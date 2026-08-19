using AutoMapper;
using BookStore.Domain.Enums;
using BookStore.Domain.Entities;
using BookStore.Application.Interfaces;
using BookStore.Application.Features.Products.Commands.CreateProduct;
using BookStore.Application.Features.Products.Responses;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles product creation.</summary>
public sealed class CreateProductHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateProductRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateProductHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="CreateProductHandler"/> class.</summary>
    public CreateProductHandler(IUnitOfWork unitOfWork, IValidator<CreateProductRequest> validator, IMapper mapper, ICurrentUserService currentUserService, ILogger<CreateProductHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<ProductResponse>> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Product creation validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<ProductResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        try
        {
            var product = ProductHandlerHelpers.CreateEntity(request.Product);
            await _unitOfWork.Products.AddAsync(product, cancellationToken);

            if (product.Quantity > 0)
            {
                await _unitOfWork.Inventory.AddAsync(
                    new InventoryTransaction(
                        product.Id,
                        product.Quantity,
                        InventoryTransactionType.InitialStock,
                        0,
                        product.Quantity,
                        "Opening balance",
                        null,
                        _currentUserService.UserId,
                        _currentUserService.FullName ?? _currentUserService.Username,
                        $"Opening stock recorded when {product.Title} was created"),
                    cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Product created: {ProductId} {Title}", product.Id, product.Title);
            return Result<ProductResponse>.Success(ProductHandlerHelpers.MapResponse(_mapper, product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while creating product");
            return Result<ProductResponse>.Failure("Unable to create product.");
        }
    }
}
