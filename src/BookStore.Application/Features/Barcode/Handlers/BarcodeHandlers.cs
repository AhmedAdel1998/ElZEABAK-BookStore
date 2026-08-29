using BookStore.Application.Features.Barcode.Commands.GenerateBarcode;
using BookStore.Application.Features.Barcode.Commands.PrintBarcode;
using BookStore.Application.Features.Barcode.Commands.ValidateBarcode;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Features.Barcode.Queries.FindProductByBarcode;
using BookStore.Application.Features.Barcode.Responses;
using BookStore.Application.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Barcode.Handlers;

/// <summary>Handles barcode generation.</summary>
public sealed class GenerateBarcodeHandler
{
    private readonly IBarcodeService _barcodeService;
    private readonly IValidator<GenerateBarcodeRequest> _validator;
    private readonly ILogger<GenerateBarcodeHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="GenerateBarcodeHandler"/> class.</summary>
    public GenerateBarcodeHandler(IBarcodeService barcodeService, IValidator<GenerateBarcodeRequest> validator, ILogger<GenerateBarcodeHandler> logger)
    {
        _barcodeService = barcodeService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<BarcodeDto>> HandleAsync(GenerateBarcodeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Barcode generation validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<BarcodeDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        var barcode = await _barcodeService.GenerateUniqueAsync(request.Format, request.Prefix, cancellationToken);
        if (request.Reserve)
        {
            barcode.IsReserved = await _barcodeService.ReserveAsync(barcode.Value, cancellationToken);
        }

        _logger.LogInformation("Barcode generated: {Barcode} {Format}", barcode.Value, barcode.Format);
        return Result<BarcodeDto>.Success(barcode);
    }
}
/// <summary>Handles barcode validation.</summary>
public sealed class ValidateBarcodeHandler
{
    private readonly IBarcodeService _barcodeService;
    private readonly IValidator<ValidateBarcodeRequest> _validator;
    private readonly ILogger<ValidateBarcodeHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ValidateBarcodeHandler"/> class.</summary>
    public ValidateBarcodeHandler(IBarcodeService barcodeService, IValidator<ValidateBarcodeRequest> validator, ILogger<ValidateBarcodeHandler> logger)
    {
        _barcodeService = barcodeService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<BarcodeValidationResponse>> HandleAsync(ValidateBarcodeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Barcode validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<BarcodeValidationResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        var valid = _barcodeService.IsValid(request.Barcode, request.Format);
        var duplicate = request.RequireUnique && await _barcodeService.IsDuplicateAsync(request.Barcode, cancellationToken);
        if (duplicate)
        {
            _logger.LogWarning("Duplicate barcode attempt: {Barcode}", request.Barcode);
        }

        return Result<BarcodeValidationResponse>.Success(new BarcodeValidationResponse
        {
            IsValid = valid && !duplicate,
            IsDuplicate = duplicate,
            Message = !valid ? "Invalid barcode." : duplicate ? "Duplicate barcode detected." : "Barcode is valid."
        });
    }
}
/// <summary>Handles barcode label print preparation.</summary>
public sealed class PrintBarcodeHandler
{
    private readonly IBarcodeLabelPrintService _printService;
    private readonly IValidator<PrintBarcodeRequest> _validator;
    private readonly ILogger<PrintBarcodeHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="PrintBarcodeHandler"/> class.</summary>
    public PrintBarcodeHandler(IBarcodeLabelPrintService printService, IValidator<PrintBarcodeRequest> validator, ILogger<PrintBarcodeHandler> logger)
    {
        _printService = printService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<OperationResult> HandleAsync(PrintBarcodeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult.Invalid(validation.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)).ToArray());
        }

        try
        {
            await _printService.PreparePrintAsync(request.Labels, cancellationToken);
            _logger.LogInformation("Barcode print completed: {Count} labels", request.Labels.Sum(label => label.Quantity));
            return OperationResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Barcode print failed");
            return OperationResult.Failure(new Error("Barcode.PrintFailed", ex.Message));
        }
    }
}

/// <summary>Handles product lookup by barcode.</summary>
public sealed class FindProductByBarcodeHandler
{
    private readonly IBarcodeService _barcodeService;
    private readonly IValidator<FindProductByBarcodeRequest> _validator;
    private readonly ILogger<FindProductByBarcodeHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="FindProductByBarcodeHandler"/> class.</summary>
    public FindProductByBarcodeHandler(IBarcodeService barcodeService, IValidator<FindProductByBarcodeRequest> validator, ILogger<FindProductByBarcodeHandler> logger)
    {
        _barcodeService = barcodeService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the request.</summary>
    public async Task<Result<BarcodeProductDto>> HandleAsync(FindProductByBarcodeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<BarcodeProductDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        var product = await _barcodeService.FindProductAsync(request.Barcode, cancellationToken);
        _logger.LogInformation("Barcode scanned: {Barcode} Found={Found}", request.Barcode, product is not null);
        return product is null ? Result<BarcodeProductDto>.Failure("Product was not found.") : Result<BarcodeProductDto>.Success(product);
    }
}
