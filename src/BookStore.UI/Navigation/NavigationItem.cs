using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BookStore.UI.Navigation;

/// <summary>
/// Represents a shell navigation menu item.
/// </summary>
public partial class NavigationItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the display text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon text.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the breadcrumb path.
    /// </summary>
    public string Breadcrumb { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the required permission.
    /// </summary>
    public string? RequiredPermission { get; set; }

    /// <summary>
    /// Gets or sets the navigation command.
    /// </summary>
    public ICommand? Command { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is active.
    /// </summary>
    [ObservableProperty]
    private bool isActive;
}
