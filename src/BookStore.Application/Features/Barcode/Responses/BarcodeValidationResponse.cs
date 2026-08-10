namespace BookStore.Application.Features.Barcode.Responses;

/// <summary>Represents barcode validation result.</summary>
public sealed class BarcodeValidationResponse
{
    /// <summary>Gets or sets a value indicating whether the barcode is valid.</summary>
    public bool IsValid { get; set; }
    /// <summary>Gets or sets a value indicating whether the barcode is duplicate.</summary>
    public bool IsDuplicate { get; set; }
    /// <summary>Gets or sets validation message.</summary>
    public string Message { get; set; } = string.Empty;
}
