using AutoMapper;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Responses;
using BookStore.Domain.Entities;
using BookStore.Domain.ValueObjects;

namespace BookStore.Application.Features.Products.Handlers;

internal static class ProductHandlerHelpers
{
    public static Product CreateEntity(ProductEditorModel model)
    {
        var product = new Product(new BookStore.Domain.ValueObjects.Barcode(model.Barcode), model.Title, model.PurchasePrice, model.SellingPrice, model.CategoryId);
        ApplyEditor(product, model);

        // Only a brand new product may have its quantity assigned directly; this is its opening
        // balance. Every later movement goes through the inventory ledger.
        product.SetQuantity(Math.Max(model.Quantity, 0));
        return product;
    }

    public static void ApplyEditor(Product product, ProductEditorModel model)
    {
        product.UpdateBarcode(new BookStore.Domain.ValueObjects.Barcode(model.Barcode));
        product.UpdateTitle(model.Title, model.Subtitle);
        product.UpdateDetails(string.IsNullOrWhiteSpace(model.ISBN) ? null : new ISBN(model.ISBN), model.Description, model.Author, model.Publisher, model.Language, model.PublishDate);
        product.UpdateBookMetadata(model.Edition, model.TaxCategory);
        product.UpdatePrice(model.PurchasePrice, model.SellingPrice);

        // Deliberately does NOT touch Quantity. Stock is owned by the inventory ledger, so editing
        // a product must never move it; use a stock adjustment instead.
        product.UpdateInventoryMetadata(model.MinimumStock, model.ShelfLocation, model.ImagePath);
        product.ChangeCategory(model.CategoryId);

        if (model.IsActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }
    }

    public static ProductResponse MapResponse(IMapper mapper, Product product) => mapper.Map<ProductResponse>(product);
}
