using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Barcode;

/// <summary>
/// Prepares barcode label print jobs for future printer-specific output.
/// </summary>
public sealed class BarcodeLabelPrintService : IBarcodeLabelPrintService
{
    private readonly ILogger<BarcodeLabelPrintService> _logger;

    /// <summary>Initializes a new instance of the <see cref="BarcodeLabelPrintService"/> class.</summary>
    public BarcodeLabelPrintService(ILogger<BarcodeLabelPrintService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PreparePrintAsync(IReadOnlyCollection<BarcodeLabelDto> labels, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Barcode label print job prepared: {LabelCount}", labels.Sum(label => label.Quantity));
        return Task.CompletedTask;
    }
}
