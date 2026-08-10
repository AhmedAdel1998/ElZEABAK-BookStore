using BookStore.Domain.Exceptions;

namespace BookStore.Domain.ValueObjects;

/// <summary>
/// Represents a non-negative monetary amount.
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Money"/> class.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code.</param>
    public Money(decimal amount, string currency = "EGP")
    {
        if (amount < 0)
        {
            throw new ValidationException("Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ValidationException("Currency is required.");
        }

        Amount = decimal.Round(amount, 2);
        Currency = currency.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Gets the amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    public string Currency { get; }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
