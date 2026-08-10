using AutoMapper;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Responses;
using BookStore.Domain.Entities;

namespace BookStore.Application.Features.Products.Mappings;

/// <summary>
/// AutoMapper profile for product module mappings.
/// </summary>
public sealed class ProductMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductMappingProfile"/> class.
    /// </summary>
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(destination => destination.Barcode, options => options.MapFrom(source => source.Barcode.Value))
            .ForMember(destination => destination.ISBN, options => options.MapFrom(source => source.ISBN == null ? null : source.ISBN.Value))
            .ForMember(destination => destination.CategoryName, options => options.MapFrom(source => source.Category == null ? null : source.Category.Name));

        CreateMap<Product, ProductListItem>()
            .IncludeBase<Product, ProductDto>();

        CreateMap<Product, ProductResponse>()
            .ForMember(destination => destination.Barcode, options => options.MapFrom(source => source.Barcode.Value))
            .ForMember(destination => destination.ISBN, options => options.MapFrom(source => source.ISBN == null ? null : source.ISBN.Value));

        CreateMap<Product, ProductEditorModel>()
            .ForMember(destination => destination.Barcode, options => options.MapFrom(source => source.Barcode.Value))
            .ForMember(destination => destination.ISBN, options => options.MapFrom(source => source.ISBN == null ? null : source.ISBN.Value))
            .ForMember(destination => destination.Author, options => options.MapFrom(source => source.Author ?? string.Empty));

        CreateMap<ProductDto, ProductEditorModel>()
            .ForMember(destination => destination.Id, options => options.MapFrom(source => source.Id))
            .ForMember(destination => destination.Author, options => options.MapFrom(source => source.Author ?? string.Empty));
    }
}
