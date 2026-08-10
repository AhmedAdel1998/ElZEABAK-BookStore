using BookStore.Application.Features.Products.Commands.ActivateProduct;
using BookStore.Application.Features.Products.Commands.CreateProduct;
using BookStore.Application.Features.Products.Commands.DeactivateProduct;
using BookStore.Application.Features.Products.Commands.DeleteProduct;
using BookStore.Application.Features.Products.Commands.DuplicateProduct;
using BookStore.Application.Features.Products.Commands.UpdateProduct;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Queries.GetLowStockProducts;
using BookStore.Application.Features.Products.Queries.GetProductById;
using BookStore.Application.Features.Products.Queries.GetProducts;
using BookStore.Application.Features.Products.Queries.SearchProducts;
using BookStore.Domain.Interfaces;
using FluentValidation;

namespace BookStore.Application.Features.Products.Validators;

/// <summary>Validates create product requests.</summary>
public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CreateProductRequestValidator"/> class.</summary>
    public CreateProductRequestValidator(IValidator<ProductEditorModel> productValidator) => RuleFor(request => request.Product).SetValidator(productValidator);
}

/// <summary>Validates update product requests.</summary>
public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateProductRequestValidator"/> class.</summary>
    public UpdateProductRequestValidator(IValidator<ProductEditorModel> productValidator)
    {
        RuleFor(request => request.Product.Id).NotEmpty().WithMessage("Product is required.");
        RuleFor(request => request.Product).SetValidator(productValidator);
    }
}

/// <summary>Validates delete product requests.</summary>
public sealed class DeleteProductRequestValidator : AbstractValidator<DeleteProductRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteProductRequestValidator"/> class.</summary>
    public DeleteProductRequestValidator() => RuleFor(request => request.Id).NotEmpty().WithMessage("Product is required.");
}

/// <summary>Validates activate product requests.</summary>
public sealed class ActivateProductRequestValidator : AbstractValidator<ActivateProductRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ActivateProductRequestValidator"/> class.</summary>
    public ActivateProductRequestValidator() => RuleFor(request => request.Id).NotEmpty().WithMessage("Product is required.");
}

/// <summary>Validates deactivate product requests.</summary>
public sealed class DeactivateProductRequestValidator : AbstractValidator<DeactivateProductRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeactivateProductRequestValidator"/> class.</summary>
    public DeactivateProductRequestValidator() => RuleFor(request => request.Id).NotEmpty().WithMessage("Product is required.");
}

/// <summary>Validates duplicate product requests.</summary>
public sealed class DuplicateProductRequestValidator : AbstractValidator<DuplicateProductRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DuplicateProductRequestValidator"/> class.</summary>
    public DuplicateProductRequestValidator(IProductRepository productRepository)
    {
        RuleFor(request => request.Id).NotEmpty().WithMessage("Product is required.");
        RuleFor(request => request.NewBarcode)
            .NotEmpty().WithMessage("Barcode is required.")
            .MustAsync(async (barcode, cancellationToken) => !await productRepository.ExistsByBarcodeAsync(barcode, null, cancellationToken))
            .WithMessage("Duplicate barcode.");
    }
}

/// <summary>Validates product-by-id queries.</summary>
public sealed class GetProductByIdRequestValidator : AbstractValidator<GetProductByIdRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetProductByIdRequestValidator"/> class.</summary>
    public GetProductByIdRequestValidator() => RuleFor(request => request.Id).NotEmpty().WithMessage("Product is required.");
}

/// <summary>Validates product list requests.</summary>
public sealed class GetProductsRequestValidator : AbstractValidator<GetProductsRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetProductsRequestValidator"/> class.</summary>
    public GetProductsRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Validates low-stock product requests.</summary>
public sealed class GetLowStockProductsRequestValidator : AbstractValidator<GetLowStockProductsRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetLowStockProductsRequestValidator"/> class.</summary>
    public GetLowStockProductsRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Validates product search requests.</summary>
public sealed class SearchProductsRequestValidator : AbstractValidator<SearchProductsRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SearchProductsRequestValidator"/> class.</summary>
    public SearchProductsRequestValidator()
    {
        RuleFor(request => request.Filter.PageNumber).GreaterThan(0);
        RuleFor(request => request.Filter.PageSize).InclusiveBetween(1, 200);
        RuleFor(request => request.Filter.SearchTerm).MaximumLength(250);
    }
}
