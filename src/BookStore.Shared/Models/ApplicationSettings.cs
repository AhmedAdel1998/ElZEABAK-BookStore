namespace BookStore.Shared.Models;

/// <summary>
/// Represents strongly typed application configuration.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Gets or sets store configuration.
    /// </summary>
    public StoreSettings Store { get; set; } = new();

    /// <summary>
    /// Gets or sets printer configuration.
    /// </summary>
    public PrinterSettings Printer { get; set; } = new();

    /// <summary>
    /// Gets or sets backup configuration.
    /// </summary>
    public BackupSettings Backup { get; set; } = new();

    /// <summary>
    /// Gets or sets logging configuration.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets user interface configuration.
    /// </summary>
    public UserInterfaceSettings UserInterface { get; set; } = new();

    /// <summary>
    /// Gets or sets authentication configuration.
    /// </summary>
    public AuthenticationSettings Authentication { get; set; } = new();

    /// <summary>
    /// Gets or sets barcode configuration.
    /// </summary>
    public BarcodeSettings Barcode { get; set; } = new();
}
