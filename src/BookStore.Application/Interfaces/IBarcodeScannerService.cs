using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Processes keyboard-wedge barcode scanner input.
/// </summary>
public interface IBarcodeScannerService
{
    /// <summary>Raised when a valid barcode is scanned.</summary>
    event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    /// <summary>Processes an input character from scanner keyboard input.</summary>
    Task ProcessInputAsync(char input, CancellationToken cancellationToken = default);

    /// <summary>Clears buffered scanner input.</summary>
    void Reset();
}

/// <summary>
/// Barcode scanned event arguments.
/// </summary>
public sealed class BarcodeScannedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="BarcodeScannedEventArgs"/> class.</summary>
    public BarcodeScannedEventArgs(string barcode, BarcodeProductDto? product)
    {
        Barcode = barcode;
        Product = product;
    }

    /// <summary>Gets barcode value.</summary>
    public string Barcode { get; }

    /// <summary>Gets matched product when found.</summary>
    public BarcodeProductDto? Product { get; }
}
