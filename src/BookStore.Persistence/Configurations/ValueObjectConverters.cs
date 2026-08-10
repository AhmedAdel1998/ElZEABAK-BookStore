using BookStore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Provides reusable value object converters.
/// </summary>
internal static class ValueObjectConverters
{
    /// <summary>
    /// Converts barcodes to and from string values.
    /// </summary>
    public static readonly ValueConverter<Barcode, string> BarcodeConverter = new(
        barcode => barcode.Value,
        value => new Barcode(value));

    /// <summary>
    /// Converts ISBN values to and from string values.
    /// </summary>
    public static readonly ValueConverter<ISBN?, string?> IsbnConverter = new(
        isbn => isbn == null ? null : isbn.Value,
        value => value == null ? null : new ISBN(value));

    /// <summary>
    /// Converts email values to and from string values.
    /// </summary>
    public static readonly ValueConverter<Email?, string?> EmailConverter = new(
        email => email == null ? null : email.Value,
        value => value == null ? null : new Email(value));

    /// <summary>
    /// Converts phone values to and from string values.
    /// </summary>
    public static readonly ValueConverter<PhoneNumber?, string?> PhoneConverter = new(
        phone => phone == null ? null : phone.Value,
        value => value == null ? null : new PhoneNumber(value));
}
