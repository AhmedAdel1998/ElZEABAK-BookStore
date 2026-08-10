namespace BookStore.Domain.ValueObjects;

/// <summary>
/// Represents a postal address.
/// </summary>
public sealed class Address : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Address"/> class.
    /// </summary>
    /// <param name="line1">Primary address line.</param>
    /// <param name="city">City name.</param>
    /// <param name="country">Country name.</param>
    /// <param name="line2">Optional secondary address line.</param>
    public Address(string line1, string city, string country, string? line2 = null)
    {
        Line1 = line1.Trim();
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
        City = city.Trim();
        Country = country.Trim();
    }

    /// <summary>
    /// Gets the primary address line.
    /// </summary>
    public string Line1 { get; }

    /// <summary>
    /// Gets the optional secondary address line.
    /// </summary>
    public string? Line2 { get; }

    /// <summary>
    /// Gets the city.
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the country.
    /// </summary>
    public string Country { get; }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return Line2;
        yield return City;
        yield return Country;
    }
}
