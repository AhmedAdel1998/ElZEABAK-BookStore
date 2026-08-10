using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.Application.Features.Barcode.Commands.ValidateBarcode;

/// <summary>Requests barcode validation.</summary>
public sealed record ValidateBarcodeRequest(string Barcode, BarcodeFormat Format = BarcodeFormat.Code128, bool RequireUnique = true);
