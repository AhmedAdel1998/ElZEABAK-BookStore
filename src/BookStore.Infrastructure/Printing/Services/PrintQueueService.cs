using System.Collections.Concurrent;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Services;

namespace BookStore.Infrastructure.Printing.Services;

public sealed class PrintQueueService : IPrintQueueService
{
    private readonly ConcurrentDictionary<Guid, ReceiptPrintJob> _retryJobs = new();
    private readonly ConcurrentDictionary<Guid, byte> _startedOrCompleted = new();

    public bool TryStart(Guid printRequestId) => _startedOrCompleted.TryAdd(printRequestId, 0);

    public void MarkCompleted(Guid printRequestId)
    {
        _retryJobs.TryRemove(printRequestId, out _);
    }

    public void EnqueueForRetry(ReceiptPrintJob job)
    {
        _retryJobs[job.RequestId] = job;
        _startedOrCompleted.TryRemove(job.RequestId, out _);
    }

    public async Task<ReceiptPrintResult?> RetryAsync(Guid printRequestId, IReceiptPrinter printer, CancellationToken cancellationToken = default)
    {
        if (!_retryJobs.TryRemove(printRequestId, out var job) || !TryStart(printRequestId))
        {
            return null;
        }

        var result = await printer.PrintAsync(job, cancellationToken);
        if (!result.Succeeded)
        {
            EnqueueForRetry(job);
        }

        return result;
    }
}
