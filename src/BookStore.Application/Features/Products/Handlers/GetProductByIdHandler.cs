using AutoMapper;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Queries.GetProductById;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>Handles product details queries.</summary>
public sealed class GetProductByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetProductByIdRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductByIdHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetProductByIdHandler"/> class.</summary>
    public GetProductByIdHandler(IUnitOfWork unitOfWork, IValidator<GetProductByIdRequest> validator, IMapper mapper, ILogger<GetProductByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<ProductDto>> HandleAsync(GetProductByIdRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Product details validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<ProductDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        return product is null
            ? Result<ProductDto>.Failure("Product was not found.")
            : Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }
}
