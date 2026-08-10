using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Provides reusable barcode generation, validation, reservation, lookup, and image generation.
/// </summary>
public interface IBarcodeService
{
    /// <summary>Generates a unique barcode.</summary>
    Task<BarcodeDto> GenerateUniqueAsync(BarcodeFormat format, string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>Validates barcode format and checksum where applicable.</summary>
    bool IsValid(string barcode, BarcodeFormat format);

    /// <summary>Checks whether a barcode is already used by a product.</summary>
    Task<bool> IsDuplicateAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>Reserves a barcode for the current process.</summary>
    Task<bool> ReserveAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>Generates SVG preview content.</summary>
    string GenerateImageSvg(string barcode, BarcodeFormat format);

    /// <summary>Finds product by barcode.</summary>
    Task<BarcodeProductDto?> FindProductAsync(string barcode, CancellationToken cancellationToken = default);
}
