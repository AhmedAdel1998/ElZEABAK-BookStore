namespace BookStore.Shared.Models;

/// <summary>
/// Represents printer settings.
/// </summary>
public class PrinterSettings
{
    /// <summary>
    /// Gets or sets the receipt printer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default receipt printer name.
    /// </summary>
    public string DefaultPrinterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets receipt paper width in millimeters.
    /// </summary>
    public int PaperWidthMm { get; set; } = 80;

    /// <summary>
    /// Gets or sets whether POS should print after checkout.
    /// </summary>
    public bool AutoPrint { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of receipt copies.
    /// </summary>
    public int Copies { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether the printer should cut paper when supported.
    /// </summary>
    public bool CutPaper { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the cash drawer should open for cash sales.
    /// </summary>
    public bool OpenCashDrawer { get; set; }

    /// <summary>
    /// Gets or sets whether the receipt logo should print when supported.
    /// </summary>
    public bool PrintLogo { get; set; }

    /// <summary>
    /// Gets or sets whether customer information should print.
    /// </summary>
    public bool PrintCustomerInformation { get; set; } = true;

    /// <summary>
    /// Gets or sets receipt footer text.
    /// </summary>
    public string ReceiptFooter { get; set; } = "No returns without receipt.";

    /// <summary>
    /// Gets or sets receipt thank-you text.
    /// </summary>
    public string ThankYouMessage { get; set; } = "Thank you for shopping with us.";

    /// <summary>
    /// Gets or sets whether QR code payload should be prepared.
    /// </summary>
    public bool PrintQrCode { get; set; }

    /// <summary>
    /// Gets or sets print timeout in milliseconds.
    /// </summary>
    public int PrintTimeoutMilliseconds { get; set; } = 10000;
}
