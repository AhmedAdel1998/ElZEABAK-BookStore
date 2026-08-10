using System.Text.RegularExpressions;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.ValueObjects;

/// <summary>
/// Represents a validated email address.
/// </summary>
public sealed partial class Email : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Email"/> class.
    /// </summary>
    /// <param name="value">The email value.</param>
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !EmailRegex().IsMatch(value))
        {
            throw new ValidationException("A valid email address is required.");
        }

        Value = value.Trim();
    }

    /// <summary>
    /// Gets the email value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
