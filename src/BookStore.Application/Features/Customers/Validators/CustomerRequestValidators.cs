using BookStore.Application.Features.Customers.Commands.ActivateCustomer;
using BookStore.Application.Features.Customers.Commands.CreateCustomer;
using BookStore.Application.Features.Customers.Commands.DeactivateCustomer;
using BookStore.Application.Features.Customers.Commands.DeleteCustomer;
using BookStore.Application.Features.Customers.Commands.UpdateCustomer;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Queries.GetCustomerById;
using BookStore.Application.Features.Customers.Queries.GetCustomerSalesHistory;
using BookStore.Application.Features.Customers.Queries.GetCustomers;
using BookStore.Application.Features.Customers.Queries.SearchCustomers;
using BookStore.Domain.Interfaces;
using FluentValidation;

namespace BookStore.Application.Features.Customers.Validators;

/// <summary>Validates customer editor models.</summary>
public sealed class CustomerEditorModelValidator : AbstractValidator<CustomerEditorModel>
{
    /// <summary>Initializes a new instance of the <see cref="CustomerEditorModelValidator"/> class.</summary>
    public CustomerEditorModelValidator(ICustomerRepository customerRepository)
    {
        RuleFor(customer => customer.FullName).NotEmpty().MaximumLength(150);
        RuleFor(customer => customer.Phone)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-]{7,20}$")
            .WithMessage("A valid phone number is required.")
            .MustAsync(async (model, phone, cancellationToken) => !await customerRepository.ExistsByPhoneAsync(phone, model.Id, cancellationToken))
            .WithMessage("Customer already exists with the same phone number.");
        RuleFor(customer => customer.Email).MaximumLength(254).EmailAddress().When(customer => !string.IsNullOrWhiteSpace(customer.Email));
        RuleFor(customer => customer.Address).MaximumLength(500);
    }
}

/// <summary>Validates customer create requests.</summary>
public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CreateCustomerRequestValidator"/> class.</summary>
    public CreateCustomerRequestValidator(IValidator<CustomerEditorModel> customerValidator) => RuleFor(request => request.Customer).SetValidator(customerValidator);
}

/// <summary>Validates customer update requests.</summary>
public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateCustomerRequestValidator"/> class.</summary>
    public UpdateCustomerRequestValidator(IValidator<CustomerEditorModel> customerValidator)
    {
        RuleFor(request => request.Customer.Id).NotEmpty().WithMessage("Customer is required.");
        RuleFor(request => request.Customer).SetValidator(customerValidator);
    }
}

/// <summary>Validates customer delete requests.</summary>
public sealed class DeleteCustomerRequestValidator : AbstractValidator<DeleteCustomerRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteCustomerRequestValidator"/> class.</summary>
    public DeleteCustomerRequestValidator() => RuleFor(request => request.Id).NotEmpty();
}

/// <summary>Validates customer activate requests.</summary>
public sealed class ActivateCustomerRequestValidator : AbstractValidator<ActivateCustomerRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ActivateCustomerRequestValidator"/> class.</summary>
    public ActivateCustomerRequestValidator() => RuleFor(request => request.Id).NotEmpty();
}

/// <summary>Validates customer deactivate requests.</summary>
public sealed class DeactivateCustomerRequestValidator : AbstractValidator<DeactivateCustomerRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeactivateCustomerRequestValidator"/> class.</summary>
    public DeactivateCustomerRequestValidator() => RuleFor(request => request.Id).NotEmpty();
}

/// <summary>Validates customer list requests.</summary>
public sealed class GetCustomersRequestValidator : AbstractValidator<GetCustomersRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetCustomersRequestValidator"/> class.</summary>
    public GetCustomersRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Validates customer search requests.</summary>
public sealed class SearchCustomersRequestValidator : AbstractValidator<SearchCustomersRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SearchCustomersRequestValidator"/> class.</summary>
    public SearchCustomersRequestValidator()
    {
        RuleFor(request => request.Filter.PageNumber).GreaterThan(0);
        RuleFor(request => request.Filter.PageSize).InclusiveBetween(1, 200);
        RuleFor(request => request.Filter.SearchTerm).MaximumLength(250);
    }
}

/// <summary>Validates customer-by-id requests.</summary>
public sealed class GetCustomerByIdRequestValidator : AbstractValidator<GetCustomerByIdRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetCustomerByIdRequestValidator"/> class.</summary>
    public GetCustomerByIdRequestValidator() => RuleFor(request => request.Id).NotEmpty();
}

/// <summary>Validates customer sales-history requests.</summary>
public sealed class GetCustomerSalesHistoryRequestValidator : AbstractValidator<GetCustomerSalesHistoryRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetCustomerSalesHistoryRequestValidator"/> class.</summary>
    public GetCustomerSalesHistoryRequestValidator()
    {
        RuleFor(request => request.CustomerId).NotEmpty();
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}
