using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.Application.Features.Barcode.Commands.PrintBarcode;

/// <summary>Requests barcode label print preparation.</summary>
public sealed record PrintBarcodeRequest(IReadOnlyCollection<BarcodeLabelDto> Labels);
