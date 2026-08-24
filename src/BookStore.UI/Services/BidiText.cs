namespace BookStore.UI.Services;

/// <summary>
/// Wraps left-to-right fragments so they survive inside right-to-left text.
/// </summary>
/// <remarks>
/// The Unicode bidirectional algorithm resolves digits and punctuation from the surrounding
/// paragraph. Inside an Arabic paragraph that reorders a fragment such as "12% (3/4)" into
/// "(3/4) %12", because the percent sign and parentheses are neutral characters that take the
/// paragraph's right-to-left direction. Isolating the fragment pins its internal order without
/// affecting the sentence around it.
/// </remarks>
public static class BidiText
{
    /// <summary>U+2066 LEFT-TO-RIGHT ISOLATE.</summary>
    private const char LeftToRightIsolate = '\u2066';

    /// <summary>U+2069 POP DIRECTIONAL ISOLATE.</summary>
    private const char PopDirectionalIsolate = '\u2069';

    /// <summary>
    /// Returns the fragment isolated as left-to-right, so numbers keep their signs, percent marks
    /// and brackets on the correct side whichever language surrounds them.
    /// </summary>
    /// <param name="fragment">The fragment to isolate.</param>
    public static string Ltr(string? fragment) =>
        string.IsNullOrEmpty(fragment) ? string.Empty : $"{LeftToRightIsolate}{fragment}{PopDirectionalIsolate}";
}
