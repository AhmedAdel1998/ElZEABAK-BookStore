using System.Text.RegularExpressions;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.ValueObjects;

/// <summary>
/// Represents a product barcode.
/// </summary>
public sealed partial class Barcode : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Barcode"/> class.
    /// </summary>
    /// <param name="value">The barcode value.</param>
    public Barcode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !BarcodeRegex().IsMatch(value))
        {
            throw new ValidationException("A valid barcode is required.");
        }

        Value = value.Trim();
    }

    /// <summary>
    /// Gets the barcode value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[A-Za-z0-9\-]{3,64}$", RegexOptions.Compiled)]
    private static partial Regex BarcodeRegex();
}
