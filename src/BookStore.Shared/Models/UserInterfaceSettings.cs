namespace BookStore.Shared.Models;

/// <summary>
/// Represents user interface settings.
/// </summary>
public class UserInterfaceSettings
{
    /// <summary>
    /// Gets or sets the selected theme.
    /// </summary>
    public string Theme { get; set; } = "Light";

    /// <summary>
    /// Gets or sets the user interface language.
    /// </summary>
    public string Language { get; set; } = "en-US";
}
