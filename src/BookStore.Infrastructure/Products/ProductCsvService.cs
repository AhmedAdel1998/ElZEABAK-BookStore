using System.Globalization;
using System.Text;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Interfaces;

namespace BookStore.Infrastructure.Products;

/// <summary>
/// Imports and exports product data using an invariant, UTF-8 CSV format.
/// </summary>
public sealed class ProductCsvService : IProductImportService, IProductExportService
{
    private const long MaximumImportBytes = 50 * 1024 * 1024;
    private static readonly string[] Headers =
    [
        "Barcode", "ISBN", "Title", "Subtitle", "Description", "Author", "Publisher",
        "Language", "Edition", "PublishDate", "PurchasePrice", "SellingPrice", "TaxCategory",
        "Quantity", "MinimumStock", "ShelfLocation", "ImagePath", "CategoryId", "IsActive"
    ];

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ProductEditorModel>> ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var file = new FileInfo(filePath);
        if (!file.Exists)
        {
            throw new FileNotFoundException("The product import file was not found.", filePath);
        }

        if (file.Length > MaximumImportBytes)
        {
            throw new InvalidDataException("The product import file cannot exceed 50 MB.");
        }

        await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
        var records = await ReadRecordsAsync(reader, cancellationToken);
        if (records.Count == 0)
        {
            throw new InvalidDataException("The product import file is empty.");
        }

        var columns = BuildColumnMap(records[0]);
        var products = new List<ProductEditorModel>(Math.Max(0, records.Count - 1));
        for (var index = 1; index < records.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = records[index];
            if (row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            try
            {
                products.Add(ParseProduct(row, columns));
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                throw new InvalidDataException($"Product import row {index + 1} is invalid: {ex.Message}", ex);
            }
        }

        return products;
    }

    /// <inheritdoc />
    public async Task ExportAsync(IEnumerable<ProductListItem> products, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("The export path has no directory."));
        var temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
            {
                await writer.WriteLineAsync(string.Join(',', Headers));
                foreach (var product in products)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var values = new object?[]
                    {
                        product.Barcode, product.ISBN, product.Title, product.Subtitle, product.Description,
                        product.Author, product.Publisher, product.Language, product.Edition,
                        product.PublishDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        product.PurchasePrice, product.SellingPrice, product.TaxCategory, product.Quantity,
                        product.MinimumStock, product.ShelfLocation, product.ImagePath, product.CategoryId,
                        product.IsActive
                    };
                    await writer.WriteLineAsync(string.Join(',', values.Select(ToCsvField)));
                }

                await writer.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string> headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headerRow.Count; index++)
        {
            var name = headerRow[index].Trim().TrimStart('\uFEFF');
            if (!map.TryAdd(name, index))
            {
                throw new InvalidDataException($"The CSV header '{name}' is duplicated.");
            }
        }

        foreach (var required in new[] { "Barcode", "Title", "Author", "PurchasePrice", "SellingPrice", "Quantity", "MinimumStock", "CategoryId" })
        {
            if (!map.ContainsKey(required))
            {
                throw new InvalidDataException($"The required CSV header '{required}' is missing.");
            }
        }

        return map;
    }

    private static ProductEditorModel ParseProduct(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> columns)
    {
        string Get(string name) => columns.TryGetValue(name, out var index) && index < row.Count ? row[index].Trim() : string.Empty;
        string? Optional(string name) => string.IsNullOrWhiteSpace(Get(name)) ? null : Get(name);
        var category = Get("CategoryId");
        if (!Guid.TryParse(category, out var categoryId) || categoryId == Guid.Empty)
        {
            throw new FormatException("CategoryId must be a non-empty GUID.");
        }

        return new ProductEditorModel
        {
            Barcode = Get("Barcode"),
            ISBN = Optional("ISBN"),
            Title = Get("Title"),
            Subtitle = Optional("Subtitle"),
            Description = Optional("Description"),
            Author = Get("Author"),
            Publisher = Optional("Publisher"),
            Language = Optional("Language"),
            Edition = Optional("Edition"),
            PublishDate = ParseOptionalDate(Get("PublishDate")),
            PurchasePrice = ParseDecimal(Get("PurchasePrice"), "PurchasePrice"),
            SellingPrice = ParseDecimal(Get("SellingPrice"), "SellingPrice"),
            TaxCategory = Optional("TaxCategory"),
            Quantity = ParseInt32(Get("Quantity"), "Quantity"),
            MinimumStock = ParseInt32(Get("MinimumStock"), "MinimumStock"),
            ShelfLocation = Optional("ShelfLocation"),
            ImagePath = Optional("ImagePath"),
            CategoryId = categoryId,
            IsActive = ParseOptionalBoolean(Get("IsActive"), defaultValue: true)
        };
    }

    private static decimal ParseDecimal(string value, string column) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new FormatException($"{column} must use an invariant numeric value (for example 12.50).");

    private static int ParseInt32(string value, string column) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new FormatException($"{column} must be a whole number.");

    private static DateOnly? ParseOptionalDate(string value) => string.IsNullOrWhiteSpace(value)
        ? null
        : DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : throw new FormatException("PublishDate must use yyyy-MM-dd.");

    private static bool ParseOptionalBoolean(string value, bool defaultValue) => string.IsNullOrWhiteSpace(value)
        ? defaultValue
        : bool.TryParse(value, out var parsed)
            ? parsed
            : throw new FormatException("IsActive must be true or false.");

    private static string ToCsvField(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
        return text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : text;
    }

    private static async Task<List<IReadOnlyList<string>>> ReadRecordsAsync(TextReader reader, CancellationToken cancellationToken)
    {
        var content = await reader.ReadToEndAsync(cancellationToken);
        var records = new List<IReadOnlyList<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var quoteClosed = false;
        for (var index = 0; index < content.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var character = content[index];
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < content.Length && content[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                        quoteClosed = true;
                    }
                }
                else
                {
                    field.Append(character);
                }
                continue;
            }

            if (character == '"' && field.Length == 0 && !quoteClosed) { inQuotes = true; continue; }
            if (character == ',') { record.Add(field.ToString()); field.Clear(); quoteClosed = false; continue; }
            if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n') index++;
                record.Add(field.ToString()); field.Clear(); quoteClosed = false;
                records.Add(record); record = [];
                continue;
            }
            if (quoteClosed)
            {
                if (!char.IsWhiteSpace(character)) throw new InvalidDataException("Unexpected data after a closing CSV quote.");
                continue;
            }
            field.Append(character);
        }

        if (inQuotes) throw new InvalidDataException("The CSV file contains an unterminated quoted field.");
        if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }
        return records;
    }
}
