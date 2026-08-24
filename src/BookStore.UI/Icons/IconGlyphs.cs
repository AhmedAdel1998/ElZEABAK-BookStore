namespace BookStore.UI.Icons;

/// <summary>
/// Maps semantic icon names to Segoe MDL2 Assets glyphs.
/// </summary>
/// <remarks>
/// Icons used to be single ASCII letters passed straight through to a TextBlock, so toolbars read as
/// stray characters and the same letter meant several different things -- "R" served Refresh,
/// Restore, Reset and Roles. Views now pass a semantic name. Anything unmapped renders as an empty
/// string rather than a literal, so an unknown name is invisible instead of wrong.
/// </remarks>
public static class IconGlyphs
{
    private static readonly IReadOnlyDictionary<string, string> Glyphs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Actions
        ["Add"] = "\uE710",
        ["Edit"] = "\uE70F",
        ["Delete"] = "\uE74D",
        ["Remove"] = "\uE738",
        ["Save"] = "\uE74E",
        ["Cancel"] = "\uE711",
        ["Close"] = "\uE711",
        ["Refresh"] = "\uE72C",
        ["Reset"] = "\uE7A7",
        ["Search"] = "\uE721",
        ["Print"] = "\uE749",
        ["Import"] = "\uE896",
        ["Export"] = "\uE898",
        ["Back"] = "\uE72B",
        ["Forward"] = "\uE72A",
        ["Duplicate"] = "\uE8C8",
        ["Generate"] = "\uE945",
        ["Validate"] = "\uE73E",
        ["Restore"] = "\uE895",
        ["Adjust"] = "\uE8AB",
        ["Filter"] = "\uE71C",

        // Areas
        ["Settings"] = "\uE713",
        ["Dashboard"] = "\uE80F",
        ["Products"] = "\uE7C3",
        ["Categories"] = "\uE8EC",
        ["Inventory"] = "\uE8EC",
        ["Customers"] = "\uE716",
        ["Suppliers"] = "\uE716",
        ["Users"] = "\uE716",
        ["Cashier"] = "\uE77B",
        ["Contact"] = "\uE77B",
        ["Reports"] = "\uE9D2",
        ["History"] = "\uE81C",
        ["Folder"] = "\uE7C3",
        ["Backup"] = "\uE78C",
        ["Barcode"] = "\uE8C7",
        ["Calendar"] = "\uE787",
        ["Logout"] = "\uE7E8",
        ["Menu"] = "\uE700",

        // Status
        ["Information"] = "\uE946",
        ["Info"] = "\uE946",
        ["Warning"] = "\uE7BA",
        ["Error"] = "\uE783",
        ["Success"] = "\uE73E",

        // Dashboard metric tiles. These reuse glyphs already verified in this map rather than
        // introducing unproven codepoints, which would render as missing-glyph boxes.
        ["Sales"] = "\uE7BF",
        ["Transactions"] = "\uE81C",
        ["Profit"] = "\uE73E",
        ["Discount"] = "\uE946",
        ["LowStock"] = "\uE7BA",
        ["OutOfStock"] = "\uE783",
        ["PurchaseValue"] = "\uE8EC",
        ["Question"] = "\uE9CE"
    };

    /// <summary>
    /// Resolves a semantic icon name to its glyph.
    /// </summary>
    /// <param name="name">The semantic icon name.</param>
    /// <returns>The glyph, or an empty string when the name is unknown or blank.</returns>
    public static string Resolve(string? name) =>
        !string.IsNullOrWhiteSpace(name) && Glyphs.TryGetValue(name.Trim(), out var glyph) ? glyph : string.Empty;
}
