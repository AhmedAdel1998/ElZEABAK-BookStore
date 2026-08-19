using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;

namespace BookStore.UI.Services;

/// <summary>
/// Makes FluentValidation's built-in messages follow the application language.
/// </summary>
/// <remarks>
/// Only 70 of the 222 validation rules set an explicit message; the rest fall back to
/// FluentValidation's defaults, which read like "'Selling Price' must be greater than '0'." Those
/// were always English. FluentValidation ships translated templates and picks them up from
/// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>, which the localization service
/// already sets - but the interpolated property name stayed English, so the sentence came out mixed.
/// Resolving display names through the dictionary closes that gap.
/// </remarks>
public static class ValidationLocalization
{
    private static readonly Regex PascalBoundary = new("(?<=[a-z0-9])(?=[A-Z])", RegexOptions.Compiled);

    /// <summary>
    /// Installs a display-name resolver that translates validated property names.
    /// </summary>
    /// <param name="localizationService">The application localization service.</param>
    public static void Apply(ILocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(localizationService);

        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) =>
            member is null ? null : localizationService.TranslateLiteral(Humanize(member.Name));
    }

    /// <summary>
    /// Turns a member name into the wording used in the resource dictionary, so
    /// <c>SellingPrice</c> resolves against the existing "Selling Price" entry.
    /// </summary>
    /// <param name="memberName">The CLR member name.</param>
    /// <returns>The spaced, human-readable name.</returns>
    internal static string Humanize(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
        {
            return memberName;
        }

        // Keep initialisms intact: ISBN stays ISBN rather than becoming I S B N.
        var spaced = PascalBoundary.Replace(memberName, " ");
        var builder = new StringBuilder(spaced.Length + 4);
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
