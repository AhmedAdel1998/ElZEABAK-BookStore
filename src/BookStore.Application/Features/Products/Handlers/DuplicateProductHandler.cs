using AutoMapper;
using BookStore.Application.Features.Products.Commands.DuplicateProduct;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Responses;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles product duplication.</summary>
public sealed class DuplicateProductHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DuplicateProductRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<DuplicateProductHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DuplicateProductHandler"/> class.</summary>
    public DuplicateProductHandler(IUnitOfWork unitOfWork, IValidator<DuplicateProductRequest> validator, IMapper mapper, ILogger<DuplicateProductHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<ProductResponse>> HandleAsync(DuplicateProductRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Product duplicate validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<ProductResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var source = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        if (source is null)
        {
            return Result<ProductResponse>.Failure("Product was not found.");
        }

        var model = _mapper.Map<ProductEditorModel>(source);
        model.Id = null;
        model.Barcode = request.NewBarcode;
        model.ISBN = null;
        model.Quantity = 0;
        model.Title = $"{model.Title} Copy";

        var product = ProductHandlerHelpers.CreateEntity(model);
        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product duplicated: {SourceProductId} -> {ProductId}", source.Id, product.Id);
        return Result<ProductResponse>.Success(ProductHandlerHelpers.MapResponse(_mapper, product));
    }
}
