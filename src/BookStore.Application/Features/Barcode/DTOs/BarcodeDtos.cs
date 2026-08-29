namespace BookStore.Application.Features.Barcode.DTOs;

/// <summary>
/// Supported barcode formats.
/// </summary>
public enum BarcodeFormat
{
    /// <summary>Code 128 barcode.</summary>
    Code128,
    /// <summary>Code 39 barcode.</summary>
    Code39,
    /// <summary>EAN-13 barcode.</summary>
    Ean13,
    /// <summary>EAN-8 barcode.</summary>
    Ean8
}

/// <summary>
/// Represents barcode data.
/// </summary>
public sealed class BarcodeDto
{
    /// <summary>Gets or sets barcode value.</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets barcode format.</summary>
    public BarcodeFormat Format { get; set; } = BarcodeFormat.Code128;
    /// <summary>Gets or sets generated image content.</summary>
    public string ImageSvg { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the barcode is reserved.</summary>
    public bool IsReserved { get; set; }
}

/// <summary>
/// Represents barcode settings.
/// </summary>
public sealed class BarcodeSettingsDto
{
    /// <summary>Gets or sets default barcode format.</summary>
    public BarcodeFormat DefaultFormat { get; set; } = BarcodeFormat.Code128;
    /// <summary>Gets or sets prefix.</summary>
    public string Prefix { get; set; } = "BK";
    /// <summary>Gets or sets starting number.</summary>
    public int StartingNumber { get; set; } = 100000;
    /// <summary>Gets or sets generated barcode length.</summary>
    public int Length { get; set; } = 12;
    /// <summary>Gets or sets label width.</summary>
    public double LabelWidthMm { get; set; } = 50;
    /// <summary>Gets or sets label height.</summary>
    public double LabelHeightMm { get; set; } = 25;
    /// <summary>Gets or sets printer name.</summary>
    public string PrinterName { get; set; } = string.Empty;
    /// <summary>Gets or sets scan timeout.</summary>
    public int ScanTimeoutMilliseconds { get; set; } = 80;
    /// <summary>Gets or sets automatic generation flag.</summary>
    public bool AutomaticGenerationEnabled { get; set; } = true;
    /// <summary>Gets or sets manual generation flag.</summary>
    public bool ManualGenerationEnabled { get; set; } = true;
}

/// <summary>
/// Represents barcode label data prepared for printing.
/// </summary>
public sealed class BarcodeLabelDto
{
    /// <summary>Gets or sets barcode value.</summary>
    public string BarcodeValue { get; set; } = string.Empty;
    /// <summary>Gets or sets product title.</summary>
    public string? ProductTitle { get; set; }
    /// <summary>Gets or sets quantity of labels.</summary>
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Represents barcode preview data.
/// </summary>
public sealed class BarcodePreviewDto
{
    /// <summary>Gets or sets barcode value.</summary>
    public string BarcodeValue { get; set; } = string.Empty;
    /// <summary>Gets or sets barcode format.</summary>
    public BarcodeFormat Format { get; set; }
    /// <summary>Gets or sets image SVG.</summary>
    public string ImageSvg { get; set; } = string.Empty;
    /// <summary>Gets or sets product title.</summary>
    public string? ProductTitle { get; set; }
}

/// <summary>
/// Represents a product found by barcode.
/// </summary>
public sealed class BarcodeProductDto
{
    /// <summary>Gets or sets product identifier.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets barcode.</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Gets or sets title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets category.</summary>
    public string? CategoryName { get; set; }
    /// <summary>Gets or sets price.</summary>
    public decimal SellingPrice { get; set; }
    /// <summary>Gets or sets quantity.</summary>
    public int Quantity { get; set; }
}
