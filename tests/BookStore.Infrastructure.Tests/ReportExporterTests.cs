using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using BookStore.Application.Features.Reports.Services;
using BookStore.Reporting.Services;

namespace BookStore.Infrastructure.Tests;

public sealed class ReportExporterTests
{
    private sealed record Row(string Product, decimal Revenue, bool Active, DateOnly Date);

    [Fact]
    public async Task CsvExport_UsesUtf8InvariantValuesAndEscapesFields()
    {
        var exporter = new ReportExporter();

        var result = await exporter.ExportAsync(new ReportExportRequest<Row>("Sales: Daily", [new Row("كتاب, \"A\"", 12.50m, true, new DateOnly(2026, 8, 28))], ReportExportFormat.Csv));
        var text = Encoding.UTF8.GetString(result.Content).TrimStart('\uFEFF');

        Assert.Equal("text/csv; charset=utf-8", result.ContentType);
        Assert.EndsWith(".csv", result.FileName);
        Assert.Contains("\"كتاب, \"\"A\"\"\",12.50,True,2026-08-28", text);
    }

    [Fact]
    public async Task ExcelExport_ProducesValidOpenXmlWorkbookWithUnicodeRows()
    {
        var exporter = new ReportExporter();

        var result = await exporter.ExportAsync(new ReportExportRequest<Row>("Sales", [new Row("كتاب", 12.50m, true, new DateOnly(2026, 8, 28))], ReportExportFormat.Excel));
        using var stream = new MemoryStream(result.Content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");

        Assert.NotNull(archive.GetEntry("[Content_Types].xml"));
        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        Assert.NotNull(sheet);
        using var reader = new StreamReader(sheet!.Open(), Encoding.UTF8);
        var xml = XDocument.Parse(await reader.ReadToEndAsync());
        Assert.Contains("كتاب", xml.ToString());
        Assert.EndsWith(".xlsx", result.FileName);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
    }
}
