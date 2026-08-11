using AutoMapper;
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
using BookStore.Application.Features.Suppliers.Responses;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using static BookStore.Application.Features.Suppliers.Handlers.SupplierHandlerHelpers;

namespace BookStore.Application.Features.Suppliers.Handlers;

/// <summary>Handles supplier creation.</summary>
public sealed class CreateSupplierHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSupplierRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSupplierHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="CreateSupplierHandler"/> class.</summary>
    public CreateSupplierHandler(IUnitOfWork unitOfWork, IValidator<CreateSupplierRequest> validator, IMapper mapper, ILogger<CreateSupplierHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SupplierResponse>> HandleAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Supplier creation validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<SupplierResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var supplier = new Supplier(request.Supplier.CompanyName);
        supplier.UpdateDetails(request.Supplier.ContactName, ToPhone(request.Supplier.Phone), ToEmail(request.Supplier.Email), ToAddress(request.Supplier.Address), request.Supplier.Notes);
        if (!request.Supplier.IsActive)
        {
            supplier.Deactivate();
        }

        await _unitOfWork.Suppliers.AddAsync(supplier, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Supplier created. SupplierId={SupplierId}", supplier.Id);
        return Result<SupplierResponse>.Success(_mapper.Map<SupplierResponse>(supplier));
    }
}

/// <summary>Handles supplier update.</summary>
public sealed class UpdateSupplierHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateSupplierRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateSupplierHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="UpdateSupplierHandler"/> class.</summary>
    public UpdateSupplierHandler(IUnitOfWork unitOfWork, IValidator<UpdateSupplierRequest> validator, IMapper mapper, ILogger<UpdateSupplierHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SupplierResponse>> HandleAsync(UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Supplier update validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<SupplierResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.Supplier.Id!.Value, cancellationToken);
        if (supplier is null)
        {
            return Result<SupplierResponse>.Failure("Supplier could not be found.");
        }

        supplier.UpdateProfile(request.Supplier.CompanyName, request.Supplier.ContactName, ToPhone(request.Supplier.Phone), ToEmail(request.Supplier.Email), ToAddress(request.Supplier.Address), request.Supplier.Notes, request.Supplier.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Supplier updated. SupplierId={SupplierId}", supplier.Id);
        return Result<SupplierResponse>.Success(_mapper.Map<SupplierResponse>(supplier));
    }
}

/// <summary>Handles supplier soft deletion.</summary>
public sealed class DeleteSupplierHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeleteSupplierRequest> _validator;
    private readonly ILogger<DeleteSupplierHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteSupplierHandler"/> class.</summary>
    public DeleteSupplierHandler(IUnitOfWork unitOfWork, IValidator<DeleteSupplierRequest> validator, ILogger<DeleteSupplierHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(DeleteSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Supplier delete validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.Id, cancellationToken);
        if (supplier is null)
        {
            return OperationResult.Failure(new Error("Supplier.NotFound", "Supplier could not be found."));
        }

        var productCount = await _unitOfWork.Suppliers.CountProductsAsync(supplier.Id, cancellationToken);
        supplier.Deactivate();
        supplier.MarkDeleted();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Supplier deleted. SupplierId={SupplierId}; ProductCount={ProductCount}", supplier.Id, productCount);
        return OperationResult.Success();
    }
}

/// <summary>Handles supplier activation.</summary>
public sealed class ActivateSupplierHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ActivateSupplierRequest> _validator;
    private readonly ILogger<ActivateSupplierHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ActivateSupplierHandler"/> class.</summary>
    public ActivateSupplierHandler(IUnitOfWork unitOfWork, IValidator<ActivateSupplierRequest> validator, ILogger<ActivateSupplierHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(ActivateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.Id, cancellationToken);
        if (supplier is null)
        {
            return OperationResult.Failure(new Error("Supplier.NotFound", "Supplier could not be found."));
        }

        supplier.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Supplier activated. SupplierId={SupplierId}", supplier.Id);
        return OperationResult.Success();
    }
}

/// <summary>Handles supplier deactivation.</summary>
public sealed class DeactivateSupplierHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeactivateSupplierRequest> _validator;
    private readonly ILogger<DeactivateSupplierHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeactivateSupplierHandler"/> class.</summary>
    public DeactivateSupplierHandler(IUnitOfWork unitOfWork, IValidator<DeactivateSupplierRequest> validator, ILogger<DeactivateSupplierHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(DeactivateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.Id, cancellationToken);
        if (supplier is null)
        {
            return OperationResult.Failure(new Error("Supplier.NotFound", "Supplier could not be found."));
        }

        supplier.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Supplier deactivated. SupplierId={SupplierId}", supplier.Id);
        return OperationResult.Success();
    }
}

/// <summary>Handles supplier list queries.</summary>
public sealed class GetSuppliersHandler
{
    private readonly SearchSuppliersHandler _searchHandler;
    private readonly IValidator<GetSuppliersRequest> _validator;

    /// <summary>Initializes a new instance of the <see cref="GetSuppliersHandler"/> class.</summary>
    public GetSuppliersHandler(SearchSuppliersHandler searchHandler, IValidator<GetSuppliersRequest> validator)
    {
        _searchHandler = searchHandler;
        _validator = validator;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<SupplierListItem>>> HandleAsync(GetSuppliersRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        return validation.IsValid
            ? await _searchHandler.HandleAsync(new SearchSuppliersRequest(new SupplierFilter { PageNumber = request.PageNumber, PageSize = request.PageSize }), cancellationToken)
            : Result<PagedResult<SupplierListItem>>.Failure(validation.Errors[0].ErrorMessage);
    }
}

/// <summary>Handles supplier search queries.</summary>
public sealed class SearchSuppliersHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SearchSuppliersRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchSuppliersHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="SearchSuppliersHandler"/> class.</summary>
    public SearchSuppliersHandler(IUnitOfWork unitOfWork, IValidator<SearchSuppliersRequest> validator, IMapper mapper, ILogger<SearchSuppliersHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<SupplierListItem>>> HandleAsync(SearchSuppliersRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Supplier search validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<PagedResult<SupplierListItem>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var filter = request.Filter;
        var items = await _unitOfWork.Suppliers.SearchAsync(filter.SearchTerm, filter.IsActive, filter.PageNumber, filter.PageSize, cancellationToken);
        var count = await _unitOfWork.Suppliers.CountAsync(filter.SearchTerm, filter.IsActive, cancellationToken);
        return Result<PagedResult<SupplierListItem>>.Success(new PagedResult<SupplierListItem>
        {
            Items = items.Select(_mapper.Map<SupplierListItem>).ToArray(),
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = count
        });
    }
}

/// <summary>Handles supplier-by-id queries.</summary>
public sealed class GetSupplierByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetSupplierByIdRequest> _validator;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of the <see cref="GetSupplierByIdHandler"/> class.</summary>
    public GetSupplierByIdHandler(IUnitOfWork unitOfWork, IValidator<GetSupplierByIdRequest> validator, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<SupplierDto>> HandleAsync(GetSupplierByIdRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SupplierDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        var supplier = await _unitOfWork.Suppliers.GetSummaryByIdAsync(request.Id, cancellationToken);
        return supplier is null ? Result<SupplierDto>.Failure("Supplier could not be found.") : Result<SupplierDto>.Success(_mapper.Map<SupplierDto>(supplier));
    }
}

/// <summary>Handles supplier-product queries.</summary>
public sealed class GetSupplierProductsHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GetSupplierProductsRequest> _validator;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of the <see cref="GetSupplierProductsHandler"/> class.</summary>
    public GetSupplierProductsHandler(IUnitOfWork unitOfWork, IValidator<GetSupplierProductsRequest> validator, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<PagedResult<SupplierProductItem>>> HandleAsync(GetSupplierProductsRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<SupplierProductItem>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var items = await _unitOfWork.Suppliers.GetProductsAsync(request.SupplierId, request.PageNumber, request.PageSize, cancellationToken);
        var count = await _unitOfWork.Suppliers.CountProductsAsync(request.SupplierId, cancellationToken);
        return Result<PagedResult<SupplierProductItem>>.Success(new PagedResult<SupplierProductItem>
        {
            Items = items.Select(_mapper.Map<SupplierProductItem>).ToArray(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = count
        });
    }
}

internal static class SupplierHandlerHelpers
{
    public static PhoneNumber ToPhone(string value) => new(value);

    public static Email? ToEmail(string? value) => string.IsNullOrWhiteSpace(value) ? null : new Email(value);

    public static Address? ToAddress(string? value) => string.IsNullOrWhiteSpace(value) ? null : new Address(value, string.Empty, string.Empty);
}
