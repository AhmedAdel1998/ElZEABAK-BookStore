using BookStore.Application.Features.Products.DTOs;
using BookStore.Infrastructure.Products;

namespace BookStore.Infrastructure.Tests;

public sealed class ProductCsvServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"BookStoreCsvTests-{Guid.NewGuid():N}");

    public ProductCsvServiceTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task ExportThenImport_PreservesQuotedUnicodeAndInvariantValues()
    {
        var service = new ProductCsvService();
        var path = Path.Combine(_directory, "products.csv");
        var categoryId = Guid.NewGuid();
        var source = new ProductListItem
        {
            Barcode = "BK,\"001\"",
            ISBN = "978-1234567890",
            Title = "هندسة البرمجيات, الجزء الأول",
            Description = "Line one\r\nLine two \"quoted\"",
            Author = "مؤلف",
            PublishDate = new DateOnly(2025, 12, 31),
            PurchasePrice = 12.50m,
            SellingPrice = 19.75m,
            Quantity = 7,
            MinimumStock = 2,
            CategoryId = categoryId,
            IsActive = true
        };

        await service.ExportAsync([source], path);
        var imported = Assert.Single(await service.ImportAsync(path));

        Assert.Equal(source.Barcode, imported.Barcode);
        Assert.Equal(source.Title, imported.Title);
        Assert.Equal(source.Description, imported.Description);
        Assert.Equal(source.PublishDate, imported.PublishDate);
        Assert.Equal(source.PurchasePrice, imported.PurchasePrice);
        Assert.Equal(source.SellingPrice, imported.SellingPrice);
        Assert.Equal(categoryId, imported.CategoryId);
    }

    [Fact]
    public async Task Import_RejectsMissingRequiredHeaderWithRowSafeMessage()
    {
        var path = Path.Combine(_directory, "invalid.csv");
        await File.WriteAllTextAsync(path, "Barcode,Title\r\nBK1,Book", new System.Text.UTF8Encoding(false));
        var service = new ProductCsvService();

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(path));

        Assert.Contains("required CSV header 'Author'", error.Message);
    }

    [Fact]
    public async Task Export_AtomicallyOverwritesExistingFile()
    {
        var path = Path.Combine(_directory, "overwrite.csv");
        await File.WriteAllTextAsync(path, "old data");
        var service = new ProductCsvService();

        await service.ExportAsync([], path);

        var content = await File.ReadAllTextAsync(path);
        Assert.StartsWith("Barcode,ISBN,Title", content.TrimStart('\uFEFF'));
        Assert.DoesNotContain("old data", content);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
