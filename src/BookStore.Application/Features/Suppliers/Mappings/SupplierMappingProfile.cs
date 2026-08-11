using AutoMapper;
using BookStore.Application.Features.Suppliers.DTOs;
using BookStore.Application.Features.Suppliers.Responses;
using BookStore.Domain.Entities;
using BookStore.Domain.ReadModels;

namespace BookStore.Application.Features.Suppliers.Mappings;

/// <summary>
/// AutoMapper profile for supplier management.
/// </summary>
public sealed class SupplierMappingProfile : Profile
{
    /// <summary>Initializes a new instance of the <see cref="SupplierMappingProfile"/> class.</summary>
    public SupplierMappingProfile()
    {
        CreateMap<SupplierListReadModel, SupplierDto>();
        CreateMap<SupplierListReadModel, SupplierListItem>();
        CreateMap<SupplierListReadModel, SupplierEditorModel>();
        CreateMap<SupplierProductReadModel, SupplierProductItem>();
        CreateMap<Supplier, SupplierResponse>();
    }
}
