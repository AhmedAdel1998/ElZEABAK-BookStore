using BookStore.Domain.Exceptions;
using BookStore.Domain.ValueObjects;

namespace BookStore.Domain.Tests;

/// <summary>
/// Pins the parsing and validation rules for the value objects that back every barcode, price,
/// email, phone number, and ISBN in the system. A regression here corrupts data silently, because
/// these types are the last line of defense before a string reaches the database.
/// </summary>
public class ValueObjectTests
{
    [Theory]
    [InlineData("ABC-123")]
    [InlineData("123456789012")]
    public void Barcode_AcceptsAlphanumericWithHyphens(string value)
    {
        var barcode = new Barcode(value);

        Assert.Equal(value, barcode.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("emoji😀code")]
    public void Barcode_RejectsInvalidValues(string value)
    {
        Assert.Throws<ValidationException>(() => new Barcode(value));
    }

    [Fact]
    public void Barcode_RejectsSurroundingWhitespace()
    {
        // The format regex runs before Trim() is applied, so a value with leading or trailing
        // whitespace fails validation rather than being cleaned up. Pinned here because it is
        // easy to "fix" by reordering the two statements without noticing the format check would
        // then also need to tolerate whitespace.
        Assert.Throws<ValidationException>(() => new Barcode("  ABC123  "));
    }

    [Fact]
    public void Barcode_EqualityIsValueBased()
    {
        Assert.Equal(new Barcode("ABC123"), new Barcode("ABC123"));
    }

    [Theory]
    [InlineData("9780306406157")]
    [InlineData("0306406152")]
    [InlineData("978-0-306-40615-7")]
    public void Isbn_AcceptsValidTenAndThirteenDigitForms(string value)
    {
        var isbn = new ISBN(value);

        Assert.DoesNotContain("-", isbn.Value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("not-an-isbn")]
    [InlineData("97803064061571")]
    public void Isbn_RejectsInvalidValues(string value)
    {
        Assert.Throws<ValidationException>(() => new ISBN(value));
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.example.co")]
    public void Email_AcceptsValidAddresses(string value)
    {
        var email = new Email(value);

        Assert.Equal(value, email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("has spaces@example.com")]
    public void Email_RejectsInvalidAddresses(string value)
    {
        Assert.Throws<ValidationException>(() => new Email(value));
    }

    [Fact]
    public void Email_EqualityIsCaseInsensitive()
    {
        Assert.Equal(new Email("User@Example.com"), new Email("user@example.com"));
    }

    [Theory]
    [InlineData("+201001001000", "+201001001000")]
    [InlineData("01001001000", "01001001000")]
    [InlineData("+20 100 100 1000", "+201001001000")]
    [InlineData("+20-100-100-1000", "+201001001000")]
    public void PhoneNumber_NormalizesToDigitsOnly(string input, string expected)
    {
        var phone = new PhoneNumber(input);

        Assert.Equal(expected, phone.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("call-me-maybe")]
    public void PhoneNumber_RejectsInvalidValues(string value)
    {
        Assert.Throws<ValidationException>(() => new PhoneNumber(value));
    }

    [Fact]
    public void Money_RoundsToTwoDecimalPlaces()
    {
        var money = new Money(19.995m);

        Assert.Equal(20.00m, money.Amount);
    }

    [Fact]
    public void Money_RejectsNegativeAmount()
    {
        Assert.Throws<ValidationException>(() => new Money(-0.01m));
    }

    [Fact]
    public void Money_NormalizesCurrencyCodeCase()
    {
        var money = new Money(10m, "egp");

        Assert.Equal("EGP", money.Currency);
    }

    [Fact]
    public void Money_DefaultsToEgpWhenCurrencyOmitted()
    {
        var money = new Money(10m);

        Assert.Equal("EGP", money.Currency);
    }

    [Fact]
    public void Money_EqualityRequiresSameAmountAndCurrency()
    {
        Assert.Equal(new Money(10m, "EGP"), new Money(10m, "EGP"));
        Assert.NotEqual(new Money(10m, "EGP"), new Money(10m, "USD"));
    }
}
