using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Barcode;

/// <summary>
/// Provides barcode generation, validation, lookup, reservation, and SVG preview services.
/// </summary>
public sealed partial class BarcodeGeneratorService : IBarcodeService
{
    private static readonly ConcurrentDictionary<string, byte> ReservedBarcodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly IProductRepository _productRepository;
    private readonly IOptions<ApplicationSettings> _settings;
    private readonly ILogger<BarcodeGeneratorService> _logger;

    /// <summary>Initializes a new instance of the <see cref="BarcodeGeneratorService"/> class.</summary>
    public BarcodeGeneratorService(IProductRepository productRepository, IOptions<ApplicationSettings> settings, ILogger<BarcodeGeneratorService> logger)
    {
        _productRepository = productRepository;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BarcodeDto> GenerateUniqueAsync(BarcodeFormat format, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var effectivePrefix = string.IsNullOrWhiteSpace(prefix) ? _settings.Value.Barcode.Prefix : prefix.Trim();
        var sequence = Math.Max(_settings.Value.Barcode.StartingNumber, 1);
        var length = Math.Clamp(_settings.Value.Barcode.Length, 3, 64);

        for (var attempt = 0; attempt < 10000; attempt++)
        {
            var body = $"{effectivePrefix}{sequence + attempt}".ToUpperInvariant();
            var candidate = format == BarcodeFormat.Ean13
                ? MakeEan(body, 13)
                : format == BarcodeFormat.Ean8
                    ? MakeEan(body, 8)
                    : body.PadLeft(Math.Min(length, 64), '0');

            if (IsValid(candidate, format) && !ReservedBarcodes.ContainsKey(candidate) && !await IsDuplicateAsync(candidate, cancellationToken))
            {
                return new BarcodeDto { Value = candidate, Format = format, ImageSvg = GenerateImageSvg(candidate, format) };
            }
        }

        _logger.LogError("Unable to generate unique barcode after maximum attempts");
        throw new InvalidOperationException("Unable to generate a unique barcode.");
    }

    /// <inheritdoc />
    public bool IsValid(string barcode, BarcodeFormat format)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return false;
        }

        return format switch
        {
            BarcodeFormat.Code128 => barcode.Length <= 64 && barcode.All(character => character >= 32 && character <= 126),
            BarcodeFormat.Code39 => Code39Regex().IsMatch(barcode),
            BarcodeFormat.Ean13 => EanRegex(13).IsMatch(barcode) && HasValidEanChecksum(barcode),
            BarcodeFormat.Ean8 => EanRegex(8).IsMatch(barcode) && HasValidEanChecksum(barcode),
            _ => false
        };
    }

    /// <inheritdoc />
    public Task<bool> IsDuplicateAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return _productRepository.ExistsByBarcodeAsync(barcode, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ReserveAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ReservedBarcodes.TryAdd(barcode, 0));
    }

    /// <inheritdoc />
    public string GenerateImageSvg(string barcode, BarcodeFormat format)
    {
        var width = Math.Max(220, barcode.Length * 14);
        var bars = new StringBuilder();
        var x = 10;
        foreach (var character in barcode)
        {
            var code = character;
            var barWidth = 1 + (code % 4);
            bars.Append(CultureInvariant($"<rect x=\"{x}\" y=\"10\" width=\"{barWidth}\" height=\"68\" fill=\"#111827\" />"));
            x += barWidth + 2;
        }

        return CultureInvariant($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"116\" viewBox=\"0 0 {width} 116\"><rect width=\"100%\" height=\"100%\" fill=\"#fff\" />{bars}<text x=\"{width / 2}\" y=\"102\" text-anchor=\"middle\" font-family=\"Consolas\" font-size=\"14\" fill=\"#111827\">{barcode}</text><text x=\"10\" y=\"92\" font-family=\"Arial\" font-size=\"10\" fill=\"#64748B\">{format}</text></svg>");
    }

    /// <inheritdoc />
    public async Task<BarcodeProductDto?> FindProductAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByBarcodeAsync(barcode, cancellationToken);
        return product is null
            ? null
            : new BarcodeProductDto
            {
                ProductId = product.Id,
                Barcode = product.Barcode.Value,
                Title = product.Title,
                CategoryName = product.Category?.Name,
                SellingPrice = product.SellingPrice,
                Quantity = product.Quantity
            };
    }

    private static string MakeEan(string input, int length)
    {
        var digits = new string(input.Where(char.IsDigit).ToArray());
        var payloadLength = length - 1;
        var payload = digits.PadLeft(payloadLength, '0')[^payloadLength..];
        return payload + CalculateEanChecksum(payload);
    }

    private static bool HasValidEanChecksum(string value)
    {
        return value.Length is 8 or 13 && value[^1] == CalculateEanChecksum(value[..^1]);
    }

    private static char CalculateEanChecksum(string payload)
    {
        var sum = 0;
        var reverse = payload.Reverse().ToArray();
        for (var index = 0; index < reverse.Length; index++)
        {
            sum += (reverse[index] - '0') * (index % 2 == 0 ? 3 : 1);
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    private static Regex EanRegex(int length) => new($"^\\d{{{length}}}$", RegexOptions.Compiled);

    private static string CultureInvariant(FormattableString value) => FormattableString.Invariant(value);

    [GeneratedRegex(@"^[A-Z0-9\-\.\ \$\/\+\%]{3,64}$", RegexOptions.Compiled)]
    private static partial Regex Code39Regex();
}
