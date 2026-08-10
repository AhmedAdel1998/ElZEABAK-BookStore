using System.Text.RegularExpressions;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.ValueObjects;

/// <summary>
/// Represents a validated phone number.
/// </summary>
public sealed partial class PhoneNumber : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumber"/> class.
    /// </summary>
    /// <param name="value">The phone number value.</param>
    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !PhoneRegex().IsMatch(value))
        {
            throw new ValidationException("A valid phone number is required.");
        }

        var trimmed = value.Trim();
        Value = trimmed.StartsWith("+", StringComparison.Ordinal)
            ? $"+{new string(trimmed.Where(char.IsDigit).ToArray())}"
            : new string(trimmed.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Gets the phone number value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^\+?[0-9\s\-]{7,20}$", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();
}
