using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Xml;
using BookStore.Application.Features.Reports.Services;

namespace BookStore.Reporting.Services;

/// <summary>Exports arbitrary report rows as UTF-8 CSV or standards-compliant XLSX workbooks.</summary>
public sealed class ReportExporter : IReportExporter
{
    private const int ExcelMaximumRows = 1_048_576;
    private const int ExcelMaximumColumns = 16_384;

    /// <inheritdoc />
    public Task<ReportExportResult> ExportAsync<TReport>(ReportExportRequest<TReport> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var rowType = typeof(TReport) == typeof(object) && request.Rows.FirstOrDefault() is { } firstRow ? firstRow.GetType() : typeof(TReport);
        if (request.Rows.Any(row => row is not null && row.GetType() != rowType)) throw new InvalidOperationException("All exported report rows must have the same type.");
        var properties = rowType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.MetadataToken)
            .ToArray();
        if (properties.Length == 0) throw new InvalidOperationException("The report row type has no exportable public properties.");
        if (properties.Length > ExcelMaximumColumns) throw new InvalidOperationException("The report exceeds Excel's column limit.");
        if (request.Rows.Count + 1 > ExcelMaximumRows) throw new InvalidOperationException("The report exceeds Excel's row limit.");
        var safeName = SanitizeFileName(request.ReportName);

        return request.Format switch
        {
            ReportExportFormat.Csv => Task.FromResult(new ReportExportResult($"{safeName}.csv", "text/csv; charset=utf-8", BuildCsv(request.Rows, properties, cancellationToken))),
            ReportExportFormat.Excel => Task.FromResult(new ReportExportResult($"{safeName}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", BuildXlsx(request.Rows, properties, cancellationToken))),
            _ => throw new ArgumentOutOfRangeException(nameof(request), "Unsupported report export format.")
        };
    }

    private static byte[] BuildCsv<TReport>(IReadOnlyCollection<TReport> rows, IReadOnlyList<PropertyInfo> properties, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        {
            writer.WriteLine(string.Join(',', properties.Select(property => Csv(property.Name))));
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.WriteLine(string.Join(',', properties.Select(property => Csv(FormatValue(property.GetValue(row))))));
            }
        }
        return stream.ToArray();
    }

    private static byte[] BuildXlsx<TReport>(IReadOnlyCollection<TReport> rows, IReadOnlyList<PropertyInfo> properties, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteTextEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            WriteTextEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            WriteTextEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Report\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            WriteTextEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sheet = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
            using var output = sheet.Open();
            using var xml = XmlWriter.Create(output, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
            xml.WriteStartDocument(true);
            xml.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            xml.WriteStartElement("sheetData");
            WriteXlsxRow(xml, 1, properties.Select(property => (object?)property.Name).ToArray());
            var rowNumber = 2;
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteXlsxRow(xml, rowNumber++, properties.Select(property => property.GetValue(row)).ToArray());
            }
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndDocument();
        }
        return stream.ToArray();
    }

    private static void WriteXlsxRow(XmlWriter xml, int rowNumber, IReadOnlyList<object?> values)
    {
        xml.WriteStartElement("row");
        xml.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            xml.WriteStartElement("c");
            xml.WriteAttributeString("r", $"{ExcelColumn(index + 1)}{rowNumber}");
            if (value is bool boolean)
            {
                xml.WriteAttributeString("t", "b");
                xml.WriteElementString("v", boolean ? "1" : "0");
            }
            else if (IsNumber(value))
            {
                xml.WriteElementString("v", FormatValue(value));
            }
            else
            {
                xml.WriteAttributeString("t", "inlineStr");
                xml.WriteStartElement("is");
                xml.WriteStartElement("t");
                xml.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                xml.WriteString(FormatValue(value));
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    private static void WriteTextEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static bool IsNumber(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        Enum enumeration => enumeration.ToString(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
    private static string Csv(string value) => value.IndexOfAny([',', '"', '\r', '\n']) < 0 ? value : $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    private static string ExcelColumn(int index)
    {
        var result = string.Empty;
        while (index > 0) { index--; result = (char)('A' + (index % 26)) + result; index /= 26; }
        return result;
    }
    private static string SanitizeFileName(string reportName)
    {
        var value = string.IsNullOrWhiteSpace(reportName) ? "report" : reportName.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '-');
        return value.Length > 120 ? value[..120] : value;
    }
}
