namespace BookStore.Application.Features.Barcode.Queries.FindProductByBarcode;

/// <summary>Requests product lookup by barcode.</summary>
public sealed record FindProductByBarcodeRequest(string Barcode);
