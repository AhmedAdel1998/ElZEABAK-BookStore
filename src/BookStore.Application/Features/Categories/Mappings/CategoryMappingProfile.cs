using AutoMapper;
using BookStore.Application.Features.Categories.DTOs;
using BookStore.Application.Features.Categories.Responses;
using BookStore.Domain.Entities;

namespace BookStore.Application.Features.Categories.Mappings;

/// <summary>
/// AutoMapper profile for category module mappings.
/// </summary>
public sealed class CategoryMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryMappingProfile"/> class.
    /// </summary>
    public CategoryMappingProfile()
    {
        CreateMap<Category, CategoryDto>()
            .ForMember(destination => destination.ProductCount, options => options.Ignore());

        CreateMap<Category, CategoryResponse>()
            .ForMember(destination => destination.ProductCount, options => options.Ignore());

        CreateMap<Category, CategoryEditorModel>();
        CreateMap<CategoryEditorModel, Category>();
    }
}
