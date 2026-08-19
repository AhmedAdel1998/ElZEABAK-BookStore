using Xunit;

namespace BookStore.Application.Tests;

/// <summary>
/// Property names are interpolated into FluentValidation's default messages, so they have to match
/// the wording used as dictionary keys for the sentence to come out fully translated.
/// </summary>
public class ValidationHumanizeTests
{
    [Theory]
    [InlineData("SellingPrice", "Selling Price")]
    [InlineData("PurchasePrice", "Purchase Price")]
    [InlineData("MinimumStock", "Minimum Stock")]
    [InlineData("CategoryId", "Category Id")]
    [InlineData("Title", "Title")]
    [InlineData("ISBN", "ISBN")]
    [InlineData("FullName", "Full Name")]
    [InlineData("ShelfLocation", "Shelf Location")]
    public void HumanizeMatchesTheDictionaryWording(string member, string expected)
    {
        Assert.Equal(expected, Humanize(member));
    }

    // Mirrors ValidationLocalization.Humanize, which lives in the WPF assembly and therefore cannot
    // be referenced from this test project.
    private static string Humanize(string memberName)
    {
        var spaced = System.Text.RegularExpressions.Regex.Replace(memberName, "(?<=[a-z0-9])(?=[A-Z])", " ");
        var builder = new System.Text.StringBuilder(spaced.Length + 4);
        for (var i = 0; i < spaced.Length; i++)
        {
            var current = spaced[i];
            if (i > 0 && char.IsUpper(current) && i + 1 < spaced.Length && char.IsLower(spaced[i + 1]) && spaced[i - 1] != ' ')
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
