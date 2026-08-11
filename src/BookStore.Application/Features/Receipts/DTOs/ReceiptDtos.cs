using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Receipts.DTOs;

public enum ReceiptPaperWidth
{
    Mm58,
    Mm80
}

public enum PrintRequestKind
{
    Automatic,
    Manual,
    Reprint,
    Test
}

public sealed class ReceiptModel
{
    public Guid SaleId { get; set; }
    public Guid PrintRequestId { get; set; } = Guid.NewGuid();
    public bool IsReprint { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? StoreAddress { get; set; }
    public string? StorePhone { get; set; }
    public string? TaxNumber { get; set; }
    public string? LogoPath { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTimeOffset SaleDate { get; set; }
    public string Cashier { get; set; } = string.Empty;
    public string RegisterName { get; set; } = Environment.MachineName;
    public string SaleStatus { get; set; } = string.Empty;
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string? CustomerPhone { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReceiptBarcode { get; set; }
    public string? ReceiptQrPayload { get; set; }
    public string Footer { get; set; } = string.Empty;
    public string ThankYouMessage { get; set; } = "Thank you for shopping with us.";
}

public sealed class ReceiptItemDto
{
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class ReceiptTemplateOptions
{
    public bool PrintStoreInformation { get; set; } = true;
    public bool PrintInvoiceInformation { get; set; } = true;
    public bool PrintCustomerInformation { get; set; } = true;
    public bool PrintItems { get; set; } = true;
    public bool PrintTotals { get; set; } = true;
    public bool PrintPayment { get; set; } = true;
    public bool PrintFooter { get; set; } = true;
    public bool PrintQrCode { get; set; }
    public bool PrintBarcode { get; set; } = true;
}

public sealed class ReceiptPrintOptions
{
    public string? PrinterName { get; set; }
    public ReceiptPaperWidth PaperWidth { get; set; } = ReceiptPaperWidth.Mm80;
    public int Copies { get; set; } = 1;
    public bool CutPaper { get; set; } = true;
    public bool OpenCashDrawer { get; set; }
    public bool PrintLogo { get; set; }
    public bool AutoPrint { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    public ReceiptTemplateOptions Template { get; set; } = new();
}

public sealed class ReceiptPrintJob
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public ReceiptModel Receipt { get; set; } = new();
    public ReceiptPrintOptions Options { get; set; } = new();
    public PrintRequestKind Kind { get; set; } = PrintRequestKind.Automatic;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ReceiptPrintResult
{
    public bool Succeeded { get; init; }
    public Guid PrintRequestId { get; init; }
    public string? PrinterName { get; init; }
    public string? Error { get; init; }
    public ReceiptModel? Receipt { get; init; }

    public static ReceiptPrintResult Success(Guid requestId, string? printerName, ReceiptModel? receipt = null) => new() { Succeeded = true, PrintRequestId = requestId, PrinterName = printerName, Receipt = receipt };
    public static ReceiptPrintResult Failure(Guid requestId, string error, string? printerName = null, ReceiptModel? receipt = null) => new() { Succeeded = false, PrintRequestId = requestId, Error = error, PrinterName = printerName, Receipt = receipt };
}

public sealed class PrinterInfoDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; }
}

public sealed class ReceiptPreviewDto
{
    public ReceiptModel Receipt { get; set; } = new();
    public string Content { get; set; } = string.Empty;
}

public sealed class ReceiptSaleSearchRowDto
{
    public Guid SaleId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTimeOffset SaleDate { get; set; }
    public string Cashier { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
