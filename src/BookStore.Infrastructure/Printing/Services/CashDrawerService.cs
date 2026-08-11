using BookStore.Application.Features.Receipts.Services;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Printing.Services;

public sealed class CashDrawerService : ICashDrawerService
{
    private readonly ILogger<CashDrawerService> _logger;

    public CashDrawerService(ILogger<CashDrawerService> logger)
    {
        _logger = logger;
    }

    public Task OpenDrawerAsync(string? printerName = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cash drawer open requested. Printer={PrinterName}", printerName);
        return Task.CompletedTask;
    }
}
