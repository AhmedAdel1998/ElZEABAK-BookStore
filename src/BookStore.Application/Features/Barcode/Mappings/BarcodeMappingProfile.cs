using AutoMapper;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Shared.Models;

namespace BookStore.Application.Features.Barcode.Mappings;

/// <summary>AutoMapper profile for barcode module.</summary>
public sealed class BarcodeMappingProfile : Profile
{
    /// <summary>Initializes a new instance of the <see cref="BarcodeMappingProfile"/> class.</summary>
    public BarcodeMappingProfile()
    {
        CreateMap<BarcodeSettings, BarcodeSettingsDto>()
            .ForMember(destination => destination.DefaultFormat, options => options.MapFrom(source => Enum.Parse<BarcodeFormat>(source.DefaultType, true)));
        CreateMap<BarcodeDto, BarcodePreviewDto>()
            .ForMember(destination => destination.BarcodeValue, options => options.MapFrom(source => source.Value));
    }
}
