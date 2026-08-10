namespace BookStore.Shared.Models;

/// <summary>
/// Represents barcode subsystem configuration.
/// </summary>
public class BarcodeSettings
{
    /// <summary>Gets or sets the default barcode type.</summary>
    public string DefaultType { get; set; } = "Code128";

    /// <summary>Gets or sets the generated barcode prefix.</summary>
    public string Prefix { get; set; } = "BK";

    /// <summary>Gets or sets the starting sequence number.</summary>
    public int StartingNumber { get; set; } = 100000;

    /// <summary>Gets or sets the barcode length.</summary>
    public int Length { get; set; } = 12;

    /// <summary>Gets or sets the label width in millimeters.</summary>
    public double LabelWidthMm { get; set; } = 50;

    /// <summary>Gets or sets the label height in millimeters.</summary>
    public double LabelHeightMm { get; set; } = 25;

    /// <summary>Gets or sets the barcode label printer name.</summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>Gets or sets scan timeout in milliseconds.</summary>
    public int ScanTimeoutMilliseconds { get; set; } = 80;

    /// <summary>Gets or sets a value indicating whether automatic generation is enabled.</summary>
    public bool AutomaticGenerationEnabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether manual generation is enabled.</summary>
    public bool ManualGenerationEnabled { get; set; } = true;
}
