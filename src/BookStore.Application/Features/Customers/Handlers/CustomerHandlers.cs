using AutoMapper;
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
using BookStore.Application.Features.Customers.Responses;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using static BookStore.Application.Features.Customers.Handlers.CustomerHandlerHelpers;

namespace BookStore.Application.Features.Customers.Handlers;

/// <summary>Handles customer creation.</summary>
public sealed class CreateCustomerHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCustomerRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCustomerHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="CreateCustomerHandler"/> class.</summary>
    public CreateCustomerHandler(IUnitOfWork unitOfWork, IValidator<CreateCustomerRequest> validator, IMapper mapper, ILogger<CreateCustomerHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<CustomerResponse>> HandleAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Customer creation validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<CustomerResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var customer = new Customer(request.Customer.FullName);
        customer.UpdateContact(ToPhone(request.Customer.Phone), ToEmail(request.Customer.Email), ToAddress(request.Customer.Address));
        if (!request.Customer.IsActive)
        {
            customer.Deactivate();
        }

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Customer created. CustomerId={CustomerId}", customer.Id);
        return Result<CustomerResponse>.Success(_mapper.Map<CustomerResponse>(customer));
    }
}

/// <summary>Handles customer update.</summary>
public sealed class UpdateCustomerHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateCustomerRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateCustomerHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="UpdateCustomerHandler"/> class.</summary>
    public UpdateCustomerHandler(IUnitOfWork unitOfWork, IValidator<UpdateCustomerRequest> validator, IMapper mapper, ILogger<UpdateCustomerHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<CustomerResponse>> HandleAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Customer update validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<CustomerResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(request.Customer.Id!.Value, cancellationToken);
        if (customer is null)
        {
            return Result<CustomerResponse>.Failure("Customer could not be found.");
        }

        customer.UpdateProfile(request.Customer.FullName, ToPhone(request.Customer.Phone), ToEmail(request.Customer.Email), ToAddress(request.Customer.Address), request.Customer.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Customer updated. CustomerId={CustomerId}", customer.Id);
        return Result<CustomerResponse>.Success(_mapper.Map<CustomerResponse>(customer));
    }
}

/// <summary>Handles customer soft deletion.</summary>
public sealed class DeleteCustomerHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeleteCustomerRequest> _validator;
    private readonly ILogger<DeleteCustomerHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteCustomerHandler"/> class.</summary>
    public DeleteCustomerHandler(IUnitOfWork unitOfWork, IValidator<DeleteCustomerRequest> validator, ILogger<DeleteCustomerHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(DeleteCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            return OperationResult.Failure(new Error("Customer.NotFound", "Customer could not be found."));
        }

        customer.Deactivate();
        customer.MarkDeleted();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Customer deleted. CustomerId={CustomerId}", customer.Id);
        return OperationResult.Success();
    }
}

/// <summary>Handles customer activation.</summary>
public sealed class ActivateCustomerHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ActivateCustomerRequest> _validator;
    private readonly ILogger<ActivateCustomerHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ActivateCustomerHandler"/> class.</summary>
    public ActivateCustomerHandler(IUnitOfWork unitOfWork, IValidator<ActivateCustomerRequest> validator, ILogger<ActivateCustomerHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(ActivateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            return OperationResult.Failure(new Error("Customer.NotFound", "Customer could not be found."));
        }

        customer.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Customer activated. CustomerId={CustomerId}", customer.Id);
        return OperationResult.Success();
    }
}

/// <summary>Handles customer deactivation.</summary>
public sealed class DeactivateCustomerHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeactivateCustomerRequest> _validator;
    private readonly ILogger<DeactivateCustomerHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeactivateCustomerHandler"/> class.</summary>
    public DeactivateCustomerHandler(IUnitOfWork unitOfWork, IValidator<DeactivateCustomerRequest> validator, ILogger<DeactivateCustomerHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(DeactivateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            return OperationResult.Failure(new Error("Customer.NotFound", "Customer could not be found."));
        }

        customer.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Customer deactivated. CustomerId={CustomerId}", customer.Id);
        return OperationResult.Success();
    }
}

/// <summary>Handles customer list queries.</summary>
public sealed class GetCustomersHandler
{
    private readonly SearchCustomersHandler _searchHandler;
    private readonly IValidator<GetCustomersRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="GetCustomersHandler"/> class.</summary>
    public GetCustomersHandler(SearchCustomersHandler searchHandler, IValidator<GetCustomersRequest> validator)
    {
        _searchHandler = searchHandler;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<CustomerListItem>>> HandleAsync(GetCustomersRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        return validation.IsValid
            ? await _searchHandler.HandleAsync(new SearchCustomersRequest(new CustomerFilter { PageNumber = request.PageNumber, PageSize = request.PageSize }), cancellationToken)
            : Result<PagedResult<CustomerListItem>>.Failure(validation.Errors[0].ErrorMessage);
    }
}

/// <summary>Handles customer search queries.</summary>
public sealed class SearchCustomersHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SearchCustomersRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchCustomersHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="SearchCustomersHandler"/> class.</summary>
    public SearchCustomersHandler(IUnitOfWork unitOfWork, IValidator<SearchCustomersRequest> validator, IMapper mapper, ILogger<SearchCustomersHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<CustomerListItem>>> HandleAsync(SearchCustomersRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Customer search validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<CustomerListItem>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var filter = request.Filter;
        var items = await _unitOfWork.Customers.SearchAsync(filter.SearchTerm, filter.IsActive, filter.PageNumber, filter.PageSize, cancellationToken);
        var count = await _unitOfWork.Customers.CountAsync(filter.SearchTerm, filter.IsActive, cancellationToken);
        return Result<PagedResult<CustomerListItem>>.Success(new PagedResult<CustomerListItem>
        {
            Items = items.Select(_mapper.Map<CustomerListItem>).ToArray(),
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = count
        });
    }
}

/// <summary>Handles customer-by-id queries.</summary>
public sealed class GetCustomerByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetCustomerByIdRequest> _validator;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of the <see cref="GetCustomerByIdHandler"/> class.</summary>
    public GetCustomerByIdHandler(IUnitOfWork unitOfWork, IValidator<GetCustomerByIdRequest> validator, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<CustomerDto>> HandleAsync(GetCustomerByIdRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<CustomerDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        var customer = await _unitOfWork.Customers.GetSummaryByIdAsync(request.Id, cancellationToken);
        return customer is null ? Result<CustomerDto>.Failure("Customer could not be found.") : Result<CustomerDto>.Success(_mapper.Map<CustomerDto>(customer));
    }
}

/// <summary>Handles customer sales-history queries.</summary>
public sealed class GetCustomerSalesHistoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetCustomerSalesHistoryRequest> _validator;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of the <see cref="GetCustomerSalesHistoryHandler"/> class.</summary>
    public GetCustomerSalesHistoryHandler(IUnitOfWork unitOfWork, IValidator<GetCustomerSalesHistoryRequest> validator, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<CustomerSaleHistoryItem>>> HandleAsync(GetCustomerSalesHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<CustomerSaleHistoryItem>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var items = await _unitOfWork.Customers.GetSalesHistoryAsync(request.CustomerId, request.DateFrom, request.DateTo, request.PageNumber, request.PageSize, cancellationToken);
        var count = await _unitOfWork.Customers.CountSalesHistoryAsync(request.CustomerId, request.DateFrom, request.DateTo, cancellationToken);
        return Result<PagedResult<CustomerSaleHistoryItem>>.Success(new PagedResult<CustomerSaleHistoryItem>
        {
            Items = items.Select(_mapper.Map<CustomerSaleHistoryItem>).ToArray(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = count
        });
    }
}

internal static class CustomerHandlerHelpers
{
    public static PhoneNumber ToPhone(string value) => new(value);

    public static Email? ToEmail(string? value) => string.IsNullOrWhiteSpace(value) ? null : new Email(value);

    public static Address? ToAddress(string? value) => string.IsNullOrWhiteSpace(value) ? null : new Address(value, string.Empty, string.Empty);
}
