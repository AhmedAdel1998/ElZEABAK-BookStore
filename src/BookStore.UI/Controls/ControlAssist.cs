using System.Windows;

namespace BookStore.UI.Controls;

/// <summary>
/// Attached properties used by reusable control templates for placeholder text, icons, and validation display.
/// </summary>
public static class ControlAssist
{
    /// <summary>
    /// Identifies the placeholder attached property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached("Placeholder", typeof(string), typeof(ControlAssist), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the icon attached property.
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached("Icon", typeof(string), typeof(ControlAssist), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the error message attached property.
    /// </summary>
    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.RegisterAttached("ErrorMessage", typeof(string), typeof(ControlAssist), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Sets placeholder text.
    /// </summary>
    public static void SetPlaceholder(DependencyObject element, string value) => element.SetValue(PlaceholderProperty, value);

    /// <summary>
    /// Gets placeholder text.
    /// </summary>
    public static string GetPlaceholder(DependencyObject element) => (string)element.GetValue(PlaceholderProperty);

    /// <summary>
    /// Sets icon text.
    /// </summary>
    public static void SetIcon(DependencyObject element, string value) => element.SetValue(IconProperty, value);

    /// <summary>
    /// Gets icon text.
    /// </summary>
    public static string GetIcon(DependencyObject element) => (string)element.GetValue(IconProperty);

    /// <summary>
    /// Sets validation error text.
    /// </summary>
    public static void SetErrorMessage(DependencyObject element, string value) => element.SetValue(ErrorMessageProperty, value);

    /// <summary>
    /// Gets validation error text.
    /// </summary>
    public static string GetErrorMessage(DependencyObject element) => (string)element.GetValue(ErrorMessageProperty);
}
