using System.Text;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Infrastructure.Printing.Models;

namespace BookStore.Infrastructure.Printing.ESCPos;

public sealed class EscPosReceiptCommandBuilder
{
    private readonly IReceiptFormatter _formatter;

    public EscPosReceiptCommandBuilder(IReceiptFormatter formatter)
    {
        _formatter = formatter;
    }

    public byte[] Build(ReceiptPrintJob job)
    {
        using var stream = new MemoryStream();
        stream.Write(EscPosCommands.Initialize);
        stream.Write(EscPosCommands.AlignCenter);
        stream.Write(EscPosCommands.BoldOn);
        WriteText(stream, job.Receipt.StoreName + Environment.NewLine);
        stream.Write(EscPosCommands.BoldOff);
        stream.Write(EscPosCommands.AlignLeft);
        WriteText(stream, _formatter.Format(job.Receipt, job.Options));

        if (job.Options.OpenCashDrawer)
        {
            stream.Write(EscPosCommands.OpenCashDrawer);
        }

        if (job.Options.CutPaper)
        {
            stream.Write(EscPosCommands.FeedAndCut);
        }

        return stream.ToArray();
    }

    private static void WriteText(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes);
    }
}
