using BookStore.Application.Features.Suppliers.Commands.ActivateSupplier;
using BookStore.Application.Features.Suppliers.Commands.CreateSupplier;
using BookStore.Application.Features.Suppliers.Commands.DeactivateSupplier;
using BookStore.Application.Features.Suppliers.Commands.DeleteSupplier;
using BookStore.Application.Features.Suppliers.Commands.UpdateSupplier;
using BookStore.Application.Features.Suppliers.DTOs;
using BookStore.Application.Features.Suppliers.Queries.GetSupplierById;
using BookStore.Application.Features.Suppliers.Queries.GetSupplierProducts;
using BookStore.Application.Features.Suppliers.Queries.GetSuppliers;
using BookStore.Application.Features.Suppliers.Queries.SearchSuppliers;
using BookStore.Domain.Interfaces;
using FluentValidation;

namespace BookStore.Application.Features.Suppliers.Validators;

/// <summary>Validates supplier editor models.</summary>
public sealed class SupplierEditorModelValidator : AbstractValidator<SupplierEditorModel>
{
    /// <summary>Initializes a new instance of the <see cref="SupplierEditorModelValidator"/> class.</summary>
    public SupplierEditorModelValidator(ISupplierRepository supplierRepository)
    {
        RuleFor(supplier => supplier.CompanyName)
            .NotEmpty()
            .MaximumLength(200)
            .MustAsync(async (model, companyName, cancellationToken) => !await supplierRepository.ExistsByCompanyNameAsync(companyName, model.Id, cancellationToken))
            .WithMessage("Supplier already exists.");
        RuleFor(supplier => supplier.ContactName).MaximumLength(150);
        RuleFor(supplier => supplier.Phone)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-]{7,20}$")
            .WithMessage("A valid phone number is required.");
        RuleFor(supplier => supplier.Email).MaximumLength(254).EmailAddress().When(supplier => !string.IsNullOrWhiteSpace(supplier.Email));
        RuleFor(supplier => supplier.Address).MaximumLength(500);
        RuleFor(supplier => supplier.Notes).MaximumLength(2000);
    }
}

/// <summary>Validates supplier create requests.</summary>
public sealed class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CreateSupplierRequestValidator"/> class.</summary>
    public CreateSupplierRequestValidator(IValidator<SupplierEditorModel> supplierValidator) => RuleFor(request => request.Supplier).SetValidator(supplierValidator);
}

/// <summary>Validates supplier update requests.</summary>
public sealed class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateSupplierRequestValidator"/> class.</summary>
    public UpdateSupplierRequestValidator(IValidator<SupplierEditorModel> supplierValidator)
    {
        RuleFor(request => request.Supplier.Id).NotEmpty().WithMessage("Supplier is required.");
        RuleFor(request => request.Supplier).SetValidator(supplierValidator);
    }
}

/// <summary>Validates supplier delete requests.</summary>
public sealed class DeleteSupplierRequestValidator : AbstractValidator<DeleteSupplierRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteSupplierRequestValidator"/> class.</summary>
    public DeleteSupplierRequestValidator() => RuleFor(request => request.Id).NotEmpty();
}

/// <summary>Validates supplier activate requests.</summary>
public sealed class ActivateSupplierRequestValidator : AbstractValidator<ActivateSupplierRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ActivateSupplierRequestValidator"/> class.</summary>
    public ActivateSupplierRequestValidator() => RuleFor(request => request.Id).NotEmpty();
}

/// <summary>Validates supplier deactivate requests.</summary>
public sealed class DeactivateSupplierRequestValidator : AbstractValidator<DeactivateSupplierRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeactivateSupplierRequestValidator"/> class.</summary>
    public DeactivateSupplierRequestValidator() => RuleFor(request => request.Id).NotEmpty();
}

/// <summary>Validates supplier list requests.</summary>
public sealed class GetSuppliersRequestValidator : AbstractValidator<GetSuppliersRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetSuppliersRequestValidator"/> class.</summary>
    public GetSuppliersRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Validates supplier search requests.</summary>
public sealed class SearchSuppliersRequestValidator : AbstractValidator<SearchSuppliersRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SearchSuppliersRequestValidator"/> class.</summary>
    public SearchSuppliersRequestValidator()
    {
        RuleFor(request => request.Filter.PageNumber).GreaterThan(0);
        RuleFor(request => request.Filter.PageSize).InclusiveBetween(1, 200);
        RuleFor(request => request.Filter.SearchTerm).MaximumLength(250);
    }
}

/// <summary>Validates supplier-by-id requests.</summary>
public sealed class GetSupplierByIdRequestValidator : AbstractValidator<GetSupplierByIdRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetSupplierByIdRequestValidator"/> class.</summary>
    public GetSupplierByIdRequestValidator() => RuleFor(request => request.Id).NotEmpty();
}

/// <summary>Validates supplier product requests.</summary>
public sealed class GetSupplierProductsRequestValidator : AbstractValidator<GetSupplierProductsRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetSupplierProductsRequestValidator"/> class.</summary>
    public GetSupplierProductsRequestValidator()
    {
        RuleFor(request => request.SupplierId).NotEmpty();
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}
