using System.Windows;
using System.Windows.Controls;

namespace BookStore.UI.Controls;

/// <summary>
/// Provides a reusable button base with icon and loading-state support.
/// </summary>
public class AppButton : Button
{
    /// <summary>
    /// Identifies the <see cref="Icon"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(AppButton), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the <see cref="IsLoading"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(AppButton), new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets the icon text displayed before the button content.
    /// </summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the button is showing a loading state.
    /// </summary>
    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }
}

/// <summary>
/// Primary command button.
/// </summary>
public class PrimaryButton : AppButton
{
}

/// <summary>
/// Secondary command button.
/// </summary>
public class SecondaryButton : AppButton
{
}

/// <summary>
/// Destructive action button.
/// </summary>
public class DangerButton : AppButton
{
}

/// <summary>
/// Positive action button.
/// </summary>
public class SuccessButton : AppButton
{
}

/// <summary>
/// Icon-only command button.
/// </summary>
public class IconButton : AppButton
{
}

/// <summary>
/// Rounded command button.
/// </summary>
public class RoundedButton : AppButton
{
}

/// <summary>
/// Outlined command button.
/// </summary>
public class OutlinedButton : AppButton
{
}
