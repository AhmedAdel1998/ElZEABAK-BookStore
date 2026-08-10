using System.Text;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Barcode;

/// <summary>
/// Processes USB barcode scanners that behave like keyboard input.
/// </summary>
public sealed class BarcodeScannerService : IBarcodeScannerService
{
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<BarcodeScannerService> _logger;
    private readonly TimeSpan _scanTimeout;
    private readonly StringBuilder _buffer = new();
    private DateTimeOffset _lastInputAt = DateTimeOffset.MinValue;

    /// <summary>Initializes a new instance of the <see cref="BarcodeScannerService"/> class.</summary>
    public BarcodeScannerService(IBarcodeService barcodeService, IOptions<ApplicationSettings> settings, ILogger<BarcodeScannerService> logger)
    {
        _barcodeService = barcodeService;
        _logger = logger;
        _scanTimeout = TimeSpan.FromMilliseconds(Math.Max(settings.Value.Barcode.ScanTimeoutMilliseconds, 10));
    }

    /// <inheritdoc />
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    /// <inheritdoc />
    public async Task ProcessInputAsync(char input, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (_buffer.Length > 0 && now - _lastInputAt > _scanTimeout)
        {
            Reset();
        }

        _lastInputAt = now;
        if (input is '\r' or '\n')
        {
            var barcode = _buffer.ToString();
            Reset();
            if (!_barcodeService.IsValid(barcode, BarcodeFormat.Code128))
            {
                _logger.LogWarning("Invalid scanner input ignored: {Barcode}", barcode);
                return;
            }

            var product = await _barcodeService.FindProductAsync(barcode, cancellationToken);
            _logger.LogInformation("Scanner barcode processed: {Barcode} Found={Found}", barcode, product is not null);
            BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(barcode, product));
            return;
        }

        _buffer.Append(input);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _buffer.Clear();
    }
}
