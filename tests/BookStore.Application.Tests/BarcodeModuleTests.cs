using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Features.Barcode.Commands.GenerateBarcode;
using BookStore.Application.Features.Barcode.Commands.ValidateBarcode;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Features.Barcode.Handlers;
using BookStore.Application.Features.Barcode.Queries.FindProductByBarcode;
using BookStore.Application.Features.Barcode.Validators;
using BookStore.Application.Interfaces;
using BookStore.Infrastructure.Barcode;
using BookStore.Shared.Constants;
using BookStore.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookStore.Application.Tests;

public class BarcodeModuleTests
{
    [Fact]
    public async Task GenerateBarcode_ReturnsUniqueBarcode()
    {
        var service = new FakeBarcodeService();
        var handler = new GenerateBarcodeHandler(service, new GenerateBarcodeRequestValidator(), NullLogger<GenerateBarcodeHandler>.Instance);

        var result = await handler.HandleAsync(new GenerateBarcodeRequest(BarcodeFormat.Code128, "BK"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("BK", result.Value!.Value);
        Assert.True(result.Value.IsReserved);
    }

    [Fact]
    public async Task ValidateBarcode_DetectsDuplicateBarcode()
    {
        var service = new FakeBarcodeService { DuplicateBarcode = "BK100" };
        var handler = new ValidateBarcodeHandler(service, new ValidateBarcodeRequestValidator(), NullLogger<ValidateBarcodeHandler>.Instance);

        var result = await handler.HandleAsync(new ValidateBarcodeRequest("BK100"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsDuplicate);
        Assert.False(result.Value.IsValid);
    }

    [Fact]
    public async Task ValidateBarcode_RejectsInvalidBarcode()
    {
        var service = new FakeBarcodeService();
        var handler = new ValidateBarcodeHandler(service, new ValidateBarcodeRequestValidator(), NullLogger<ValidateBarcodeHandler>.Instance);

        var result = await handler.HandleAsync(new ValidateBarcodeRequest(""));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ScannerInputProcessing_RaisesEventWhenEnterTerminatesScan()
    {
        var service = new FakeBarcodeService { Product = new BarcodeProductDto { Barcode = "BK100", Title = "Book", ProductId = Guid.NewGuid() } };
        var scanner = new BarcodeScannerService(service, Options.Create(new ApplicationSettings()), NullLogger<BarcodeScannerService>.Instance);
        BarcodeScannedEventArgs? scanned = null;
        scanner.BarcodeScanned += (_, args) => scanned = args;

        foreach (var input in "BK100\n")
        {
            await scanner.ProcessInputAsync(input);
        }

        Assert.NotNull(scanned);
        Assert.Equal("BK100", scanned!.Barcode);
        Assert.Equal("Book", scanned.Product?.Title);
    }

    [Fact]
    public async Task FindProductByBarcode_ReturnsProduct()
    {
        var service = new FakeBarcodeService { Product = new BarcodeProductDto { Barcode = "BK100", Title = "Book", ProductId = Guid.NewGuid() } };
        var handler = new FindProductByBarcodeHandler(service, new FindProductByBarcodeRequestValidator(), NullLogger<FindProductByBarcodeHandler>.Instance);

        var result = await handler.HandleAsync(new FindProductByBarcodeRequest("BK100"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Book", result.Value!.Title);
    }

    [Fact]
    public void PermissionChecks_UseBarcodePermissionConstants()
    {
        var authorization = new FakeAuthorizationService([PermissionConstants.BarcodeView, PermissionConstants.BarcodeGenerate]);

        Assert.True(authorization.HasPermission(PermissionConstants.BarcodeView));
        Assert.True(authorization.HasPermission(PermissionConstants.BarcodeGenerate));
        Assert.False(authorization.HasPermission(PermissionConstants.BarcodePrint));
    }

    [Fact]
    public async Task BarcodeSettingsValidation_RejectsInvalidTimeout()
    {
        var validator = new BarcodeSettingsDtoValidator();

        var result = await validator.ValidateAsync(new BarcodeSettingsDto { ScanTimeoutMilliseconds = 1 });

        Assert.False(result.IsValid);
    }

    private sealed class FakeBarcodeService : IBarcodeService
    {
        public string? DuplicateBarcode { get; set; }
        public BarcodeProductDto? Product { get; set; }

        public Task<BarcodeDto> GenerateUniqueAsync(BarcodeFormat format, string? prefix = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BarcodeDto { Value = $"{prefix ?? "BK"}100", Format = format, ImageSvg = "<svg />" });
        }

        public bool IsValid(string barcode, BarcodeFormat format) => !string.IsNullOrWhiteSpace(barcode);
        public Task<bool> IsDuplicateAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(string.Equals(barcode, DuplicateBarcode, StringComparison.OrdinalIgnoreCase));
        public Task<bool> ReserveAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public string GenerateImageSvg(string barcode, BarcodeFormat format) => "<svg />";
        public Task<BarcodeProductDto?> FindProductAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(Product);
    }

    private sealed class FakeAuthorizationService(IReadOnlyCollection<string> permissions) : IAuthorizationService
    {
        public bool HasPermission(string permission) => permissions.Contains(permission);
        public bool HasPermissions(params string[] permissionsToCheck) => permissionsToCheck.All(HasPermission);
        public bool HasRole(string role) => false;
        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
    }
}
