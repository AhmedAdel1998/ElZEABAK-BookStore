using System.Text;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Features.Settings.Services;
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
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<BarcodeScannerService> _logger;
    private readonly int _fallbackScanTimeout;
    private readonly StringBuilder _buffer = new();
    private DateTimeOffset _lastInputAt = DateTimeOffset.MinValue;

    /// <summary>Initializes a new instance of the <see cref="BarcodeScannerService"/> class.</summary>
    public BarcodeScannerService(IBarcodeService barcodeService, ISettingsService settingsService, ILogger<BarcodeScannerService> logger)
    {
        _barcodeService = barcodeService;
        _settingsService = settingsService;
        _logger = logger;
        _fallbackScanTimeout = 80;
    }

    /// <summary>Initializes a scanner with static settings for isolated hosts and tests.</summary>
    public BarcodeScannerService(IBarcodeService barcodeService, IOptions<ApplicationSettings> settings, ILogger<BarcodeScannerService> logger)
    {
        _barcodeService = barcodeService;
        _logger = logger;
        _fallbackScanTimeout = settings.Value.Barcode.ScanTimeoutMilliseconds;
    }

    /// <inheritdoc />
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    /// <inheritdoc />
    public async Task ProcessInputAsync(char input, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var timeoutMilliseconds = _settingsService is null
            ? _fallbackScanTimeout
            : (await _settingsService.GetAsync<BookStore.Application.Features.Settings.DTOs.BarcodeSettingsDto>(cancellationToken)).ScanTimeout;
        var scanTimeout = TimeSpan.FromMilliseconds(Math.Clamp(timeoutMilliseconds, 20, 5000));
        if (_buffer.Length > 0 && now - _lastInputAt > scanTimeout)
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
