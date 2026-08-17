using System.Windows;

namespace BookStore.UI.Services;

/// <summary>
/// Applies runtime localization to loaded WPF visual trees.
/// </summary>
public interface IUiTreeLocalizer
{
    /// <summary>Attaches localization behavior to a root element.</summary>
    void Attach(FrameworkElement root);
}
