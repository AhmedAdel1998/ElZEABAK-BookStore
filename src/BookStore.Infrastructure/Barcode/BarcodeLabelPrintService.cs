using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Barcode;

/// <summary>Sends standards-compliant Code 128-B barcode labels to a Windows print queue.</summary>
[SupportedOSPlatform("windows6.1")]
public sealed class BarcodeLabelPrintService : IBarcodeLabelPrintService
{
    private static readonly string[] Code128Patterns =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213", "221312", "231212",
        "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132", "221231", "213212", "223112", "312131",
        "311222", "321122", "321221", "312212", "322112", "322211", "212123", "212321", "232121", "111323", "131123", "131321",
        "112313", "132113", "132311", "211313", "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121",
        "313121", "211331", "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214", "112412", "122114",
        "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111", "111242", "121142", "121241", "114212",
        "124112", "124211", "411212", "421112", "421211", "212141", "214121", "412121", "111143", "111341", "131141", "114113",
        "114311", "411113", "411311", "113141", "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
    ];

    private readonly ISettingsService _settingsService;
    private readonly IBarcodeService _barcodeService;
    private readonly IPrinterDiscoveryService _printerDiscoveryService;
    private readonly ILogger<BarcodeLabelPrintService> _logger;

    /// <summary>Initializes a barcode label printer.</summary>
    public BarcodeLabelPrintService(ISettingsService settingsService, IBarcodeService barcodeService, IPrinterDiscoveryService printerDiscoveryService, ILogger<BarcodeLabelPrintService> logger)
    {
        _settingsService = settingsService;
        _barcodeService = barcodeService;
        _printerDiscoveryService = printerDiscoveryService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PreparePrintAsync(IReadOnlyCollection<BarcodeLabelDto> labels, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(labels);
        var expanded = labels.SelectMany(label => Enumerable.Repeat(label, label.Quantity)).ToArray();
        if (expanded.Length == 0) throw new InvalidOperationException("At least one barcode label is required.");
        if (expanded.Length > 10_000) throw new InvalidOperationException("A barcode print job cannot exceed 10,000 labels.");
        var settings = await _settingsService.GetAsync<BookStore.Application.Features.Settings.DTOs.BarcodeSettingsDto>(cancellationToken);
        var format = Enum.TryParse<BarcodeFormat>(settings.DefaultFormat, true, out var configuredFormat) ? configuredFormat : BarcodeFormat.Code128;
        if (expanded.Any(label => !_barcodeService.IsValid(label.BarcodeValue, format)))
        {
            throw new InvalidOperationException($"One or more label values are invalid for {format}.");
        }

        var printerName = settings.PrinterName;
        if (string.IsNullOrWhiteSpace(printerName)) printerName = (await _printerDiscoveryService.GetDefaultPrinterAsync(cancellationToken))?.Name;
        if (string.IsNullOrWhiteSpace(printerName) || !await _printerDiscoveryService.IsPrinterAvailableAsync(printerName, cancellationToken))
        {
            throw new InvalidOperationException("The configured barcode printer is unavailable.");
        }

        var width = Math.Clamp(settings.LabelWidthMm, 10, 300);
        var height = Math.Clamp(settings.LabelHeightMm, 10, 300);
        await Task.Run(() => Print(printerName, expanded, width, height, format), cancellationToken);
        _logger.LogInformation("Barcode label print job completed. Printer={PrinterName} LabelCount={LabelCount}", printerName, expanded.Length);
    }

    private static void Print(string printerName, IReadOnlyList<BarcodeLabelDto> labels, double widthMm, double heightMm, BarcodeFormat format)
    {
        using var document = new PrintDocument();
        document.DocumentName = "BookStore Barcode Labels";
        document.PrinterSettings.PrinterName = printerName;
        if (!document.PrinterSettings.IsValid) throw new InvalidOperationException("The configured barcode printer is invalid.");
        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.DefaultPageSettings.PaperSize = new PaperSize("BookStore Barcode Label", Math.Max(1, (int)Math.Round(widthMm / 25.4 * 100)), Math.Max(1, (int)Math.Round(heightMm / 25.4 * 100)));
        var index = 0;
        document.PrintPage += (_, args) =>
        {
            DrawLabel(args.Graphics ?? throw new InvalidOperationException("The printer did not provide a graphics surface."), args.PageBounds, labels[index], format);
            index++;
            args.HasMorePages = index < labels.Count;
        };
        document.Print();
    }

    private static void DrawLabel(Graphics graphics, Rectangle page, BarcodeLabelDto label, BarcodeFormat format)
    {
        graphics.Clear(Color.White);
        using var titleFont = new Font("Arial", 8, FontStyle.Bold, GraphicsUnit.Point);
        using var valueFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(Color.Black);
        using var centered = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        var title = string.IsNullOrWhiteSpace(label.ProductTitle) ? string.Empty : label.ProductTitle.Trim();
        var titleHeight = string.IsNullOrEmpty(title) ? 2f : Math.Min(18f, page.Height * 0.18f);
        if (!string.IsNullOrEmpty(title)) graphics.DrawString(title, titleFont, brush, new RectangleF(4, 1, Math.Max(1, page.Width - 8), titleHeight), centered);
        var textHeight = Math.Min(16f, page.Height * 0.18f);
        var barcodeBounds = new RectangleF(4, titleHeight + 2, Math.Max(1, page.Width - 8), Math.Max(1, page.Height - titleHeight - textHeight - 5));
        if (format == BarcodeFormat.Code39) DrawCode39(graphics, label.BarcodeValue, barcodeBounds);
        else if (format is BarcodeFormat.Ean13 or BarcodeFormat.Ean8) DrawEan(graphics, label.BarcodeValue, barcodeBounds, format);
        else DrawCode128B(graphics, label.BarcodeValue, barcodeBounds);
        graphics.DrawString(label.BarcodeValue, valueFont, brush, new RectangleF(2, page.Height - textHeight - 1, Math.Max(1, page.Width - 4), textHeight), centered);
    }

    private static void DrawCode128B(Graphics graphics, string value, RectangleF bounds)
    {
        const int startB = 104;
        var codes = value.Select(character => character - 32).ToList();
        var checksum = startB;
        for (var index = 0; index < codes.Count; index++) checksum += codes[index] * (index + 1);
        codes.Insert(0, startB);
        codes.Add(checksum % 103);
        codes.Add(106);
        var modules = codes.Sum(code => Code128Patterns[code].Sum(character => character - '0')) + 20;
        var moduleWidth = bounds.Width / modules;
        var x = bounds.Left + (10 * moduleWidth);
        using var barBrush = new SolidBrush(Color.Black);
        foreach (var code in codes)
        {
            var pattern = Code128Patterns[code];
            for (var element = 0; element < pattern.Length; element++)
            {
                var elementWidth = (pattern[element] - '0') * moduleWidth;
                if (element % 2 == 0) graphics.FillRectangle(barBrush, x, bounds.Top, Math.Max(0.5f, elementWidth), bounds.Height);
                x += elementWidth;
            }
        }
    }

    private static void DrawEan(Graphics graphics, string value, RectangleF bounds, BarcodeFormat format)
    {
        string[] left = ["0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011"];
        string[] alternate = ["0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111"];
        string[] right = ["1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100"];
        string pattern;
        if (format == BarcodeFormat.Ean8)
        {
            pattern = "101" + string.Concat(value[..4].Select(character => left[character - '0'])) + "01010" + string.Concat(value[4..].Select(character => right[character - '0'])) + "101";
        }
        else
        {
            string[] parity = ["LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"];
            var leftPattern = parity[value[0] - '0'];
            var encodedLeft = string.Concat(value.Substring(1, 6).Select((character, index) => leftPattern[index] == 'L' ? left[character - '0'] : alternate[character - '0']));
            pattern = "101" + encodedLeft + "01010" + string.Concat(value[7..].Select(character => right[character - '0'])) + "101";
        }
        DrawBinaryPattern(graphics, pattern, bounds, 10);
    }

    private static void DrawCode39(Graphics graphics, string value, RectangleF bounds)
    {
        const string characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";
        string[] patterns =
        [
            "nnnwwnwnn", "wnnwnnnnw", "nnwwnnnnw", "wnwwnnnnn", "nnnwwnnnw", "wnnwwnnnn", "nnwwwnnnn", "nnnwnnwnw", "wnnwnnwnn", "nnwwnnwnn",
            "wnnnnwnnw", "nnwnnwnnw", "wnwnnwnnn", "nnnnwwnnw", "wnnnwwnnn", "nnwnwwnnn", "nnnnnwwnw", "wnnnnwwnn", "nnwnnwwnn", "nnnnwwwnn",
            "wnnnnnnww", "nnwnnnnww", "wnwnnnnwn", "nnnnwnnww", "wnnnwnnwn", "nnwnwnnwn", "nnnnnnwww", "wnnnnnwwn", "nnwnnnwwn", "nnnnwnwwn",
            "wwnnnnnnw", "nwwnnnnnw", "wwwnnnnnn", "nwnnwnnnw", "wwnnwnnnn", "nwwnwnnnn", "nwnnnnwnw", "wwnnnnwnn", "nwwnnnwnn", "nwnwnwnnn",
            "nwnwnnnwn", "nwnnnwnwn", "nnnwnwnwn", "nwnnwnwnn"
        ];
        var encoded = $"*{value}*";
        var modules = encoded.Sum(character => patterns[characters.IndexOf(character, StringComparison.Ordinal)].Sum(width => width == 'w' ? 3 : 1)) + encoded.Length - 1 + 20;
        var unit = bounds.Width / modules;
        var x = bounds.Left + (10 * unit);
        using var brush = new SolidBrush(Color.Black);
        for (var characterIndex = 0; characterIndex < encoded.Length; characterIndex++)
        {
            var pattern = patterns[characters.IndexOf(encoded[characterIndex], StringComparison.Ordinal)];
            for (var element = 0; element < pattern.Length; element++)
            {
                var width = (pattern[element] == 'w' ? 3 : 1) * unit;
                if (element % 2 == 0) graphics.FillRectangle(brush, x, bounds.Top, Math.Max(0.5f, width), bounds.Height);
                x += width;
            }
            if (characterIndex + 1 < encoded.Length) x += unit;
        }
    }

    private static void DrawBinaryPattern(Graphics graphics, string pattern, RectangleF bounds, int quietModules)
    {
        var unit = bounds.Width / (pattern.Length + (quietModules * 2));
        var x = bounds.Left + (quietModules * unit);
        using var brush = new SolidBrush(Color.Black);
        foreach (var bit in pattern)
        {
            if (bit == '1') graphics.FillRectangle(brush, x, bounds.Top, Math.Max(0.5f, unit), bounds.Height);
            x += unit;
        }
    }
}
