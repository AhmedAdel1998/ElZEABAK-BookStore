using BookStore.Application.Features.Barcode.Commands.GenerateBarcode;
using BookStore.Application.Features.Barcode.Commands.PrintBarcode;
using BookStore.Application.Features.Barcode.Commands.ValidateBarcode;
using BookStore.Application.Features.Barcode.Queries.FindProductByBarcode;
using BookStore.Application.Features.Barcode.DTOs;
using FluentValidation;

namespace BookStore.Application.Features.Barcode.Validators;

/// <summary>Validates barcode generation requests.</summary>
public sealed class GenerateBarcodeRequestValidator : AbstractValidator<GenerateBarcodeRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GenerateBarcodeRequestValidator"/> class.</summary>
    public GenerateBarcodeRequestValidator() => RuleFor(request => request.Prefix).MaximumLength(12);
}

/// <summary>Validates barcode validation requests.</summary>
public sealed class ValidateBarcodeRequestValidator : AbstractValidator<ValidateBarcodeRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ValidateBarcodeRequestValidator"/> class.</summary>
    public ValidateBarcodeRequestValidator()
    {
        RuleFor(request => request.Barcode).NotEmpty().WithMessage("Barcode is required.").MaximumLength(64);
    }
}

/// <summary>Validates barcode print requests.</summary>
public sealed class PrintBarcodeRequestValidator : AbstractValidator<PrintBarcodeRequest>
{
    /// <summary>Initializes a new instance of the <see cref="PrintBarcodeRequestValidator"/> class.</summary>
    public PrintBarcodeRequestValidator()
    {
        RuleFor(request => request.Labels).NotEmpty().WithMessage("At least one label is required.");
        RuleForEach(request => request.Labels).ChildRules(label =>
        {
            label.RuleFor(item => item.BarcodeValue).NotEmpty().WithMessage("Barcode is required.");
            label.RuleFor(item => item.Quantity).InclusiveBetween(1, 1000);
        });
    }
}

/// <summary>Validates barcode product lookup requests.</summary>
public sealed class FindProductByBarcodeRequestValidator : AbstractValidator<FindProductByBarcodeRequest>
{
    /// <summary>Initializes a new instance of the <see cref="FindProductByBarcodeRequestValidator"/> class.</summary>
    public FindProductByBarcodeRequestValidator() => RuleFor(request => request.Barcode).NotEmpty().WithMessage("Barcode is required.").MaximumLength(64);
}

/// <summary>Validates barcode settings DTOs.</summary>
public sealed class BarcodeSettingsDtoValidator : AbstractValidator<BarcodeSettingsDto>
{
    /// <summary>Initializes a new instance of the <see cref="BarcodeSettingsDtoValidator"/> class.</summary>
    public BarcodeSettingsDtoValidator()
    {
        RuleFor(settings => settings.Prefix).MaximumLength(12);
        RuleFor(settings => settings.StartingNumber).GreaterThanOrEqualTo(0);
        RuleFor(settings => settings.Length).InclusiveBetween(3, 64);
        RuleFor(settings => settings.LabelWidthMm).GreaterThan(0);
        RuleFor(settings => settings.LabelHeightMm).GreaterThan(0);
        RuleFor(settings => settings.ScanTimeoutMilliseconds).InclusiveBetween(10, 5000);
    }
}
