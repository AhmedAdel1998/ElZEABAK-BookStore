using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Infrastructure.Printing.Models;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Printing.Services;

/// <summary>Sends the ESC/POS cash-drawer pulse to a Windows printer queue.</summary>
[SupportedOSPlatform("windows6.1")]
public sealed class CashDrawerService : ICashDrawerService
{
    private readonly IPrinterDiscoveryService _printerDiscoveryService;
    private readonly ILogger<CashDrawerService> _logger;

    public CashDrawerService(IPrinterDiscoveryService printerDiscoveryService, ILogger<CashDrawerService> logger)
    {
        _printerDiscoveryService = printerDiscoveryService;
        _logger = logger;
    }

    public async Task OpenDrawerAsync(string? printerName = null, CancellationToken cancellationToken = default)
    {
        var resolved = string.IsNullOrWhiteSpace(printerName) ? (await _printerDiscoveryService.GetDefaultPrinterAsync(cancellationToken))?.Name : printerName.Trim();
        if (string.IsNullOrWhiteSpace(resolved) || !await _printerDiscoveryService.IsPrinterAvailableAsync(resolved, cancellationToken))
        {
            throw new InvalidOperationException("The cash-drawer printer is unavailable.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Task.Run(() => SendRaw(resolved, EscPosCommands.OpenCashDrawer), cancellationToken);
        _logger.LogInformation("Cash drawer pulse sent. Printer={PrinterName}", resolved);
    }

    private static void SendRaw(string printerName, byte[] data)
    {
        if (!OpenPrinter(printerName, out var printer, IntPtr.Zero)) ThrowLastWin32("Unable to open the printer.");
        try
        {
            var document = new DocInfo { DocumentName = "BookStore Cash Drawer", DataType = "RAW" };
            if (StartDocPrinter(printer, 1, document) == 0) ThrowLastWin32("Unable to start the cash-drawer print job.");
            try
            {
                if (!StartPagePrinter(printer)) ThrowLastWin32("Unable to start the cash-drawer printer page.");
                try
                {
                    if (!WritePrinter(printer, data, data.Length, out var written) || written != data.Length) ThrowLastWin32("Unable to send the complete cash-drawer command.");
                }
                finally { _ = EndPagePrinter(printer); }
            }
            finally { _ = EndDocPrinter(printer); }
        }
        finally { _ = ClosePrinter(printer); }
    }

    private static void ThrowLastWin32(string message) => throw new Win32Exception(Marshal.GetLastWin32Error(), message);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string DocumentName = string.Empty;
        [MarshalAs(UnmanagedType.LPWStr)] public string? OutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string DataType = "RAW";
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool OpenPrinter(string printerName, out IntPtr printer, IntPtr defaults);
    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ClosePrinter(IntPtr printer);
    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int StartDocPrinter(IntPtr printer, int level, [In] DocInfo document);
    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EndDocPrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool StartPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EndPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool WritePrinter(IntPtr printer, [In] byte[] bytes, int count, out int written);
}
