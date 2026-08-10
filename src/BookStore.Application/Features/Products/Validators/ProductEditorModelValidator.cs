using BookStore.Application.Features.Products.DTOs;
using BookStore.Domain.Interfaces;
using FluentValidation;

namespace BookStore.Application.Features.Products.Validators;

/// <summary>
/// Validates product editor models.
/// </summary>
public sealed class ProductEditorModelValidator : AbstractValidator<ProductEditorModel>
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductEditorModelValidator"/> class.
    /// </summary>
    public ProductEditorModelValidator(IProductRepository productRepository)
    {
        RuleFor(product => product.Barcode)
            .NotEmpty().WithMessage("Barcode is required.")
            .MustAsync(async (product, barcode, cancellationToken) => !await productRepository.ExistsByBarcodeAsync(barcode, product.Id, cancellationToken))
            .WithMessage("Duplicate barcode.");

        RuleFor(product => product.ISBN)
            .Must(IsValidIsbn).When(product => !string.IsNullOrWhiteSpace(product.ISBN))
            .WithMessage("Invalid ISBN.")
            .MustAsync(async (product, isbn, cancellationToken) => string.IsNullOrWhiteSpace(isbn) || !await productRepository.ExistsByIsbnAsync(isbn, product.Id, cancellationToken))
            .WithMessage("Duplicate ISBN.");

        RuleFor(product => product.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title cannot exceed 250 characters.");

        RuleFor(product => product.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MaximumLength(150).WithMessage("Author cannot exceed 150 characters.");

        RuleFor(product => product.Publisher).MaximumLength(150).WithMessage("Publisher cannot exceed 150 characters.");
        RuleFor(product => product.PurchasePrice).GreaterThanOrEqualTo(0).WithMessage("Purchase price cannot be negative.");
        RuleFor(product => product.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.");
        RuleFor(product => product.Quantity).GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
        RuleFor(product => product.MinimumStock).GreaterThanOrEqualTo(0).WithMessage("Minimum stock cannot be negative.");
        RuleFor(product => product.CategoryId).NotEmpty().WithMessage("Category is required.");
        RuleFor(product => product.ImagePath).Must(HaveAllowedImageExtension).WithMessage("Image must be JPG, PNG, or WEBP.");
        RuleFor(product => product.ImagePath).Must(HaveAllowedImageSize).WithMessage("Image cannot exceed 5 MB.");
    }

    private static bool IsValidIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return true;
        }

        var normalized = isbn.Replace("-", string.Empty, StringComparison.Ordinal).Trim();
        return normalized.Length is 10 or 13 && normalized.Take(normalized.Length - 1).All(char.IsDigit) && (char.IsDigit(normalized[^1]) || normalized[^1] is 'X' or 'x');
    }

    private static bool HaveAllowedImageExtension(string? imagePath)
    {
        return string.IsNullOrWhiteSpace(imagePath) || AllowedImageExtensions.Contains(Path.GetExtension(imagePath), StringComparer.OrdinalIgnoreCase);
    }

    private static bool HaveAllowedImageSize(string? imagePath)
    {
        return string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath) || new FileInfo(imagePath).Length <= MaxImageBytes;
    }
}
