using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Prepares barcode label printing without implementing receipt printing.
/// </summary>
public interface IBarcodeLabelPrintService
{
    /// <summary>Prepares label output for printing.</summary>
    Task PreparePrintAsync(IReadOnlyCollection<BarcodeLabelDto> labels, CancellationToken cancellationToken = default);
}
