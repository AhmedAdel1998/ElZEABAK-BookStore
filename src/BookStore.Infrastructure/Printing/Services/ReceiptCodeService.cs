using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Services;

namespace BookStore.Infrastructure.Printing.Services;

public sealed class ReceiptCodeService : IReceiptCodeService
{
    public string BuildBarcode(ReceiptModel receipt) => receipt.InvoiceNumber;

    public string? BuildQrPayload(ReceiptModel receipt)
    {
        return string.IsNullOrWhiteSpace(receipt.InvoiceNumber)
            ? null
            : $"invoice={receipt.InvoiceNumber};total={receipt.GrandTotal:N2};date={receipt.SaleDate:yyyyMMddHHmmss}";
    }
}
