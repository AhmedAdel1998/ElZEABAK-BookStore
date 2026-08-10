using AutoMapper;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Responses;
using BookStore.Domain.Entities;
using BookStore.Domain.ReadModels;

namespace BookStore.Application.Features.Customers.Mappings;

/// <summary>
/// AutoMapper profile for customer management.
/// </summary>
public sealed class CustomerMappingProfile : Profile
{
    /// <summary>Initializes a new instance of the <see cref="CustomerMappingProfile"/> class.</summary>
    public CustomerMappingProfile()
    {
        CreateMap<CustomerListReadModel, CustomerDto>();
        CreateMap<CustomerListReadModel, CustomerListItem>();
        CreateMap<CustomerListReadModel, CustomerSelectionItem>();
        CreateMap<CustomerListReadModel, CustomerEditorModel>();
        CreateMap<Customer, CustomerResponse>();
        CreateMap<CustomerSaleHistoryReadModel, CustomerSaleHistoryItem>()
            .ForMember(destination => destination.PaymentMethod, options => options.MapFrom(source => source.PaymentMethod.ToString()))
            .ForMember(destination => destination.SaleStatus, options => options.MapFrom(source => source.SaleStatus.ToString()));
    }
}
