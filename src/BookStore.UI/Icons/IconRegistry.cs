namespace BookStore.UI.Icons;

/// <summary>
/// Provides centralized icon keys for navigation, actions, and status indicators.
/// </summary>
public interface IIconRegistry
{
    /// <summary>
    /// Gets an icon glyph by key.
    /// </summary>
    /// <param name="key">The icon key.</param>
    /// <returns>The matching glyph or a fallback glyph.</returns>
    string Get(string key);
}

/// <summary>
/// Default in-memory icon registry used by reusable UI controls.
/// </summary>
public sealed class IconRegistry : IIconRegistry
{
    private readonly IReadOnlyDictionary<string, string> _icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Dashboard"] = "D",
        ["Categories"] = "C",
        ["Products"] = "P",
        ["Inventory"] = "I",
        ["Sales"] = "S",
        ["Customers"] = "U",
        ["Suppliers"] = "V",
        ["Reports"] = "R",
        ["Settings"] = "G",
        ["Users"] = "U",
        ["Roles"] = "R",
        ["Backup"] = "B",
        ["Logout"] = "L",
        ["Search"] = "Q",
        ["Information"] = "i",
        ["Warning"] = "!",
        ["Error"] = "x",
        ["Success"] = "+",
        ["Question"] = "?"
    };

    /// <inheritdoc />
    public string Get(string key) => _icons.TryGetValue(key, out var icon) ? icon : "?";
}
