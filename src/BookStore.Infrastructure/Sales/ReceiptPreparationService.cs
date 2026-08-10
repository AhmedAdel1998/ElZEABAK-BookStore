using System.Text.Json;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Sales;

/// <summary>
/// Prepares receipt payloads for a later printing module without sending them to a printer.
/// </summary>
public sealed class ReceiptPreparationService : IReceiptPreparationService
{
    private readonly ILogger<ReceiptPreparationService> _logger;

    /// <summary>Initializes a new instance of the <see cref="ReceiptPreparationService"/> class.</summary>
    public ReceiptPreparationService(ILogger<ReceiptPreparationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PrepareAsync(ReceiptModel receipt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var tempFolder = Path.Combine(AppContext.BaseDirectory, FolderConstants.Temp);
        Directory.CreateDirectory(tempFolder);
        var safeInvoice = string.Join("_", receipt.InvoiceNumber.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var path = Path.Combine(tempFolder, $"receipt-{safeInvoice}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, receipt, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
        _logger.LogInformation("Receipt prepared for invoice {InvoiceNumber}. Path={Path}", receipt.InvoiceNumber, path);
    }
}
