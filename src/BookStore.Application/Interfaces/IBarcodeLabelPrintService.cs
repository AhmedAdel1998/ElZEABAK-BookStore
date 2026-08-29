using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Sends barcode labels to the configured label printer.
/// </summary>
public interface IBarcodeLabelPrintService
{
    /// <summary>Prints label output.</summary>
    Task PreparePrintAsync(IReadOnlyCollection<BarcodeLabelDto> labels, CancellationToken cancellationToken = default);
}
