using AutoMapper;
using BookStore.Application.Features.Inventory.DTOs;
using BookStore.Domain.Entities;

namespace BookStore.Application.Features.Inventory.Mappings;

/// <summary>
/// AutoMapper profile for inventory module mappings.
/// </summary>
public sealed class InventoryMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryMappingProfile"/> class.
    /// </summary>
    public InventoryMappingProfile()
    {
        CreateMap<Product, InventoryItemDto>()
            .ForMember(destination => destination.ProductId, options => options.MapFrom(source => source.Id))
            .ForMember(destination => destination.Barcode, options => options.MapFrom(source => source.Barcode.Value))
            .ForMember(destination => destination.ISBN, options => options.MapFrom(source => source.ISBN == null ? null : source.ISBN.Value))
            .ForMember(destination => destination.CategoryName, options => options.MapFrom(source => source.Category == null ? null : source.Category.Name))
            .ForMember(destination => destination.CurrentQuantity, options => options.MapFrom(source => source.Quantity))
            .ForMember(destination => destination.LastUpdated, options => options.MapFrom(source => source.UpdatedAt));

        CreateMap<InventoryTransaction, InventoryTransactionDto>()
            .ForMember(destination => destination.QuantityChange, options => options.MapFrom(source => source.Quantity))
            .ForMember(destination => destination.User, options => options.MapFrom(source => source.UserName));
    }
}
