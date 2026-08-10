using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.Application.Features.Barcode.Commands.GenerateBarcode;

/// <summary>Requests barcode generation.</summary>
public sealed record GenerateBarcodeRequest(BarcodeFormat Format = BarcodeFormat.Code128, string? Prefix = null, bool Reserve = true);
