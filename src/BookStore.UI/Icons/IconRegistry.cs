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
    /// <inheritdoc />
    public string Get(string key) => IconGlyphs.Resolve(key);

}
