using System.Windows;
using System.Windows.Controls;

namespace BookStore.UI.Controls;

/// <summary>
/// Reusable content card for dashboard metrics, information blocks, statistics, and quick actions.
/// </summary>
public class AppCard : ContentControl
{
    /// <summary>
    /// Identifies the <see cref="Title"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(AppCard), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the <see cref="Subtitle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(AppCard), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the <see cref="Icon"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(AppCard), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the <see cref="Value"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(AppCard), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the <see cref="Footer"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(nameof(Footer), typeof(object), typeof(AppCard), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the card title.
    /// </summary>
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>
    /// Gets or sets the card subtitle.
    /// </summary>
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

    /// <summary>
    /// Gets or sets the card icon.
    /// </summary>
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>
    /// Gets or sets the primary card value.
    /// </summary>
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>
    /// Gets or sets optional footer content.
    /// </summary>
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }
}

/// <summary>
/// Loading spinner control used by overlays and busy indicators.
/// </summary>
public class LoadingSpinner : ProgressBar
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingSpinner"/> class.
    /// </summary>
    public LoadingSpinner()
    {
        IsIndeterminate = true;
        Width = 120;
        Height = 8;
    }
}

/// <summary>
/// Reusable loading overlay bound to an operation state.
/// </summary>
public class LoadingOverlay : ContentControl
{
    /// <summary>
    /// Identifies the <see cref="IsBusy"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(LoadingOverlay), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the <see cref="Message"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay), new PropertyMetadata("Loading..."));

    /// <summary>
    /// Gets or sets a value indicating whether the overlay is visible.
    /// </summary>
    public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    /// <summary>
    /// Gets or sets the overlay message.
    /// </summary>
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
}

/// <summary>
/// Compact busy indicator for forms and toolbars.
/// </summary>
public class BusyIndicator : ContentControl
{
    /// <summary>
    /// Identifies the <see cref="IsBusy"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(BusyIndicator), new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets a value indicating whether the indicator is active.
    /// </summary>
    public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }
}

/// <summary>
/// Lightweight skeleton placeholder used while list or card data is loading.
/// </summary>
public class SkeletonLoader : Control
{
}

/// <summary>
/// Reusable data grid base with loading, empty-state, and pagination metadata.
/// </summary>
public class AppDataGrid : DataGrid
{
    /// <summary>
    /// Identifies the <see cref="IsLoading"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(AppDataGrid), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the <see cref="EmptyMessage"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EmptyMessageProperty =
        DependencyProperty.Register(nameof(EmptyMessage), typeof(string), typeof(AppDataGrid), new PropertyMetadata("No records found."));

    /// <summary>
    /// Identifies the <see cref="IsPaginationVisible"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsPaginationVisibleProperty =
        DependencyProperty.Register(nameof(IsPaginationVisible), typeof(bool), typeof(AppDataGrid), new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets a value indicating whether grid data is loading.
    /// </summary>
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    /// <summary>
    /// Gets or sets the empty state message.
    /// </summary>
    public string EmptyMessage { get => (string)GetValue(EmptyMessageProperty); set => SetValue(EmptyMessageProperty, value); }

    /// <summary>
    /// Gets or sets a value indicating whether pagination controls should be displayed by a hosting view.
    /// </summary>
    public bool IsPaginationVisible { get => (bool)GetValue(IsPaginationVisibleProperty); set => SetValue(IsPaginationVisibleProperty, value); }
}

/// <summary>
/// Shared empty-state view for pages and panels.
/// </summary>
public class EmptyStateView : ContentControl
{
    /// <summary>
    /// Identifies the <see cref="Title"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyStateView), new PropertyMetadata("No data"));

    /// <summary>
    /// Identifies the <see cref="Message"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(EmptyStateView), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the empty-state title.
    /// </summary>
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>
    /// Gets or sets the empty-state message.
    /// </summary>
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
}

/// <summary>
/// Alias control for no-data states.
/// </summary>
public class NoDataView : EmptyStateView
{
}
