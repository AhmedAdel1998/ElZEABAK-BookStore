using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Products.Handlers;

/// <summary>
/// Validates and imports an entire product batch in one database transaction.
/// </summary>
public sealed class ImportProductsHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ProductEditorModel> _validator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ImportProductsHandler> _logger;

    /// <summary>Initializes a new product batch import handler.</summary>
    public ImportProductsHandler(
        IUnitOfWork unitOfWork,
        IValidator<ProductEditorModel> validator,
        ICurrentUserService currentUserService,
        ILogger<ImportProductsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>Imports the supplied products atomically.</summary>
    public async Task<Result<int>> HandleAsync(IReadOnlyCollection<ProductEditorModel> products, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(products);
        if (products.Count == 0)
        {
            return Result<int>.Failure("The import file contains no product rows.");
        }

        var duplicateBarcode = products
            .GroupBy(product => product.Barcode.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateBarcode is not null)
        {
            return Result<int>.Failure($"Barcode '{duplicateBarcode.Key}' appears more than once in the import file.");
        }

        var duplicateIsbn = products
            .Where(product => !string.IsNullOrWhiteSpace(product.ISBN))
            .GroupBy(product => product.ISBN!.Replace("-", string.Empty, StringComparison.Ordinal).Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateIsbn is not null)
        {
            return Result<int>.Failure($"ISBN '{duplicateIsbn.Key}' appears more than once in the import file.");
        }

        var rowNumber = 1;
        foreach (var product in products)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;
            var validation = await _validator.ValidateAsync(product, cancellationToken);
            if (!validation.IsValid)
            {
                return Result<int>.Failure($"Row {rowNumber}: {validation.Errors[0].ErrorMessage}");
            }
        }

        foreach (var categoryId in products.Select(product => product.CategoryId).Distinct())
        {
            if (await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken) is null)
            {
                return Result<int>.Failure($"Category '{categoryId}' does not exist.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var model in products)
            {
                var product = ProductHandlerHelpers.CreateEntity(model);
                await _unitOfWork.Products.AddAsync(product, cancellationToken);
                if (product.Quantity > 0)
                {
                    await _unitOfWork.Inventory.AddAsync(
                        new InventoryTransaction(
                            product.Id,
                            product.Quantity,
                            InventoryTransactionType.InitialStock,
                            0,
                            product.Quantity,
                            "Imported opening balance",
                            null,
                            _currentUserService.UserId,
                            _currentUserService.FullName ?? _currentUserService.Username,
                            $"Opening stock recorded when {product.Title} was imported"),
                        cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            _logger.LogInformation("Product import completed atomically: {ProductCount} products", products.Count);
            return Result<int>.Success(products.Count);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Product import failed and was rolled back");
            return Result<int>.Failure("The product import failed; no products were added.");
        }
    }
}
