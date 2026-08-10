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
        return product;
    }

    public static void ApplyEditor(Product product, ProductEditorModel model)
    {
        product.UpdateBarcode(new BookStore.Domain.ValueObjects.Barcode(model.Barcode));
        product.UpdateTitle(model.Title, model.Subtitle);
        product.UpdateDetails(string.IsNullOrWhiteSpace(model.ISBN) ? null : new ISBN(model.ISBN), model.Description, model.Author, model.Publisher, model.Language, model.PublishDate);
        product.UpdateBookMetadata(model.Edition, model.TaxCategory);
        product.UpdatePrice(model.PurchasePrice, model.SellingPrice);
        product.SetQuantity(model.Quantity);
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
