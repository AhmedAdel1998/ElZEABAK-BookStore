using System.Text.RegularExpressions;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.ValueObjects;

/// <summary>
/// Represents an ISBN value.
/// </summary>
public sealed partial class ISBN : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ISBN"/> class.
    /// </summary>
    /// <param name="value">The ISBN value.</param>
    public ISBN(string value)
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal).Trim();
        if (!IsbnRegex().IsMatch(normalized))
        {
            throw new ValidationException("A valid ISBN-10 or ISBN-13 value is required.");
        }

        Value = normalized;
    }

    /// <summary>
    /// Gets the normalized ISBN value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^(?:\d{9}[\dXx]|\d{13})$", RegexOptions.Compiled)]
    private static partial Regex IsbnRegex();
}
