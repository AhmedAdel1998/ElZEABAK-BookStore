using System.ComponentModel;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using BookStore.UI.Controls;
using BookStore.UI.Navigation;

namespace BookStore.UI.Services;

/// <summary>
/// Translates static WPF text that was not authored with DynamicResource keys.
/// </summary>
/// <remarks>
/// <para>
/// Coverage is driven by three explicit triggers rather than by the Loaded event. Loaded is a
/// <see cref="RoutingStrategy.Direct"/> event, so a handler on the shell window is never invoked
/// for descendants, and class handlers were measured not to be invoked for it either. Relying on
/// it left every page reached by navigation untranslated. Instead the attached root is localized
/// when it loads, and re-localized whenever the culture changes or navigation swaps the content
/// region.
/// </para>
/// <para>
/// Two rules keep this safe to run over a live screen: a property carrying a binding or a
/// DynamicResource is never written to (doing so tears the value down and freezes live data), and
/// the authored value is recorded on first visit so switching back to English restores the
/// original rather than guessing a reverse translation.
/// </para>
/// </remarks>
public sealed class UiTreeLocalizer : IUiTreeLocalizer
{
    private static readonly DependencyProperty OriginalTextProperty =
        DependencyProperty.RegisterAttached("OriginalText", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalContentProperty =
        DependencyProperty.RegisterAttached("OriginalContent", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalHeaderProperty =
        DependencyProperty.RegisterAttached("OriginalHeader", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalPlaceholderProperty =
        DependencyProperty.RegisterAttached("OriginalPlaceholder", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalTitleProperty =
        DependencyProperty.RegisterAttached("OriginalTitle", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalMessageProperty =
        DependencyProperty.RegisterAttached("OriginalMessage", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalSubtitleProperty =
        DependencyProperty.RegisterAttached("OriginalSubtitle", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalToolTipProperty =
        DependencyProperty.RegisterAttached("OriginalToolTip", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalEmptyMessageProperty =
        DependencyProperty.RegisterAttached("OriginalEmptyMessage", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalErrorMessageProperty =
        DependencyProperty.RegisterAttached("OriginalErrorMessage", typeof(string), typeof(UiTreeLocalizer), new PropertyMetadata(null));

    private readonly ILocalizationService _localizationService;
    private readonly List<WeakReference<FrameworkElement>> _attachedRoots = [];
    private bool _walkQueued;

    /// <summary>Initializes a new instance of the <see cref="UiTreeLocalizer"/> class.</summary>
    /// <param name="localizationService">Supplies translations and culture change notifications.</param>
    /// <param name="navigationService">Top-level navigation between login, setup, and the shell.</param>
    /// <param name="shellNavigationService">Navigation inside the authenticated content region.</param>
    public UiTreeLocalizer(
        ILocalizationService localizationService,
        INavigationService navigationService,
        IShellNavigationService shellNavigationService)
    {
        _localizationService = localizationService;
        _localizationService.CultureChanged += (_, _) => ScheduleWalk();

        // Navigation is the only way new views enter the tree, and both services raise
        // PropertyChanged for CurrentViewModel, so this covers every page without depending on
        // the Loaded event.
        ObserveNavigation(navigationService);
        ObserveNavigation(shellNavigationService);
    }

    /// <inheritdoc />
    public void Attach(FrameworkElement root)
    {
        if (TryPurgeAndFind(root))
        {
            return;
        }

        _attachedRoots.Add(new WeakReference<FrameworkElement>(root));

        // The culture is applied during startup, before this root exists, so there is no culture
        // change left to react to. Queue a pass for the tree this root is about to build, and also
        // hook Loaded in case the root is attached before it is shown.
        root.Loaded += OnRootLoaded;
        ScheduleWalk();
    }

    private void OnRootLoaded(object sender, RoutedEventArgs e) => ScheduleWalk();

    private void ObserveNavigation(INavigationService? navigationService)
    {
        if (navigationService is INotifyPropertyChanged observable)
        {
            observable.PropertyChanged += OnNavigationPropertyChanged;
        }
    }

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(INavigationService.CurrentViewModel))
        {
            ScheduleWalk();
        }
    }

    /// <summary>
    /// Queues a single pass over the attached roots at Loaded priority, which runs after WPF has
    /// built the visual tree for the new content. Repeated requests inside one dispatcher turn
    /// coalesce, so a navigation that also changes the breadcrumb only walks once.
    /// </summary>
    private void ScheduleWalk()
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            LocalizeRoots();
            return;
        }

        if (_walkQueued)
        {
            return;
        }

        _walkQueued = true;
        application.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            () =>
            {
                _walkQueued = false;
                LocalizeRoots();
            });
    }

    private void LocalizeRoots()
    {
        foreach (var root in SnapshotRoots())
        {
            LocalizeSubtree(root);
        }
    }

    /// <summary>
    /// Drops roots that have been collected and reports whether <paramref name="root"/> is already
    /// attached. Roots are held weakly so closed windows do not pin their visual trees for the
    /// life of this singleton.
    /// </summary>
    private bool TryPurgeAndFind(FrameworkElement root)
    {
        var found = false;
        for (var i = _attachedRoots.Count - 1; i >= 0; i--)
        {
            if (!_attachedRoots[i].TryGetTarget(out var existing))
            {
                _attachedRoots.RemoveAt(i);
                continue;
            }

            if (ReferenceEquals(existing, root))
            {
                found = true;
            }
        }

        return found;
    }

    private List<FrameworkElement> SnapshotRoots()
    {
        var roots = new List<FrameworkElement>(_attachedRoots.Count);
        for (var i = _attachedRoots.Count - 1; i >= 0; i--)
        {
            if (_attachedRoots[i].TryGetTarget(out var root))
            {
                roots.Add(root);
            }
            else
            {
                _attachedRoots.RemoveAt(i);
            }
        }

        return roots;
    }

    private void LocalizeSubtree(DependencyObject element)
    {
        LocalizeSelf(element);

        // A context menu and a tooltip live in their own popup trees, so the visual walk below never
        // reaches them. Every context menu in the app stayed English because of this.
        if (element is FrameworkElement framework)
        {
            if (framework.ContextMenu is { } contextMenu)
            {
                LocalizeMenuItems(contextMenu.Items);
            }

            LocalizeToolTip(framework);
        }

        var count = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < count; i++)
        {
            LocalizeSubtree(VisualTreeHelper.GetChild(element, i));
        }
    }

    /// <summary>
    /// Translates menu headers, recursing into submenus. <see cref="MenuItem"/> derives from
    /// <see cref="HeaderedItemsControl"/>, not <see cref="HeaderedContentControl"/>, so it matched no
    /// case in <see cref="LocalizeSelf"/>.
    /// </summary>
    private void LocalizeMenuItems(System.Collections.IEnumerable items)
    {
        foreach (var item in items)
        {
            if (item is not MenuItem menuItem)
            {
                continue;
            }

            if (menuItem.Header is string)
            {
                Apply(menuItem, HeaderedItemsControl.HeaderProperty, OriginalHeaderProperty, (string)menuItem.Header, value => menuItem.Header = value);
            }

            LocalizeMenuItems(menuItem.Items);
        }
    }

    /// <summary>Translates a plain string tooltip, leaving richer tooltip content alone.</summary>
    private void LocalizeToolTip(FrameworkElement element)
    {
        if (element.ToolTip is string tip)
        {
            Apply(element, FrameworkElement.ToolTipProperty, OriginalToolTipProperty, tip, value => element.ToolTip = value);
        }
    }

    /// <summary>
    /// Translates the inline runs of a text block. Text assembled from &lt;Run&gt; elements is not
    /// reachable through the visual tree, so pagination footers and detail summaries stayed English.
    /// </summary>
    private void LocalizeInlines(TextBlock textBlock)
    {
        foreach (var inline in textBlock.Inlines.ToArray())
        {
            if (inline is Run run)
            {
                Apply(run, Run.TextProperty, OriginalTextProperty, run.Text, value => run.Text = value);
            }
        }
    }

    private void LocalizeSelf(DependencyObject element)
    {
        switch (element)
        {
            case TextBlock textBlock:
                // Writing Text over a block built from inline runs destroys those runs (including any
                // bound ones), so pick whichever the author actually used.
                if (DependencyPropertyHelper.GetValueSource(textBlock, TextBlock.TextProperty).BaseValueSource == BaseValueSource.Default
                    && textBlock.Inlines.Count > 0)
                {
                    LocalizeInlines(textBlock);
                }
                else
                {
                    Apply(textBlock, TextBlock.TextProperty, OriginalTextProperty, textBlock.Text, value => textBlock.Text = value);
                }

                break;
            case TextInputBox input:
                Apply(input, TextInputBox.PlaceholderProperty, OriginalPlaceholderProperty, input.Placeholder, value => input.Placeholder = value);
                Apply(input, TextInputBox.ErrorMessageProperty, OriginalErrorMessageProperty, input.ErrorMessage, value => input.ErrorMessage = value);
                break;
            case MenuItem menuItem:
                if (menuItem.Header is string menuHeader)
                {
                    Apply(menuItem, HeaderedItemsControl.HeaderProperty, OriginalHeaderProperty, menuHeader, value => menuItem.Header = value);
                }

                LocalizeMenuItems(menuItem.Items);
                break;
            case LoadingOverlay overlay:
                Apply(overlay, LoadingOverlay.MessageProperty, OriginalMessageProperty, overlay.Message, value => overlay.Message = value);
                LocalizeContent(overlay);
                break;
            case AppDataGrid appDataGrid:
                Apply(appDataGrid, AppDataGrid.EmptyMessageProperty, OriginalEmptyMessageProperty, appDataGrid.EmptyMessage, value => appDataGrid.EmptyMessage = value);
                LocalizeDataGrid(appDataGrid);
                break;
            case EmptyStateView emptyState:
                Apply(emptyState, EmptyStateView.TitleProperty, OriginalTitleProperty, emptyState.Title, value => emptyState.Title = value);
                Apply(emptyState, EmptyStateView.MessageProperty, OriginalMessageProperty, emptyState.Message, value => emptyState.Message = value);
                LocalizeContent(emptyState);
                break;
            case AppCard card:
                Apply(card, AppCard.TitleProperty, OriginalTitleProperty, card.Title, value => card.Title = value);
                Apply(card, AppCard.SubtitleProperty, OriginalSubtitleProperty, card.Subtitle, value => card.Subtitle = value);
                LocalizeContent(card);
                break;
            case HeaderedContentControl headered:
                LocalizeHeader(headered);
                LocalizeContent(headered);
                break;
            case ContentControl contentControl:
                LocalizeContent(contentControl);
                break;
            case DataGrid dataGrid:
                LocalizeDataGrid(dataGrid);
                break;
        }
    }

    private void LocalizeContent(ContentControl control)
    {
        if (control.Content is not string content || IsDataGeneratedContainer(control))
        {
            return;
        }

        Apply(control, ContentControl.ContentProperty, OriginalContentProperty, content, value => control.Content = value);
    }

    private void LocalizeHeader(HeaderedContentControl control)
    {
        if (control.Header is not string header)
        {
            return;
        }

        Apply(control, HeaderedContentControl.HeaderProperty, OriginalHeaderProperty, header, value => control.Header = value);
    }

    private void LocalizeDataGrid(DataGrid dataGrid)
    {
        foreach (var column in dataGrid.Columns)
        {
            if (column.Header is not string header)
            {
                continue;
            }

            Apply(column, DataGridColumn.HeaderProperty, OriginalHeaderProperty, header, value => column.Header = value);
        }
    }

    /// <summary>
    /// Reports whether this control is an item container filled from a bound collection. The
    /// generator assigns the data item to Content as a plain local value, indistinguishable from
    /// an authored literal, so records that happen to match a dictionary entry would be rewritten.
    /// Items authored in XAML (an <see cref="ItemsControl"/> with no ItemsSource) stay translatable.
    /// </summary>
    private static bool IsDataGeneratedContainer(ContentControl control) =>
        ItemsControl.ItemsControlFromItemContainer(control) is { ItemsSource: not null };

    /// <summary>
    /// Records the authored value once, then writes either its translation or the recorded
    /// original. Bound and empty properties are left alone.
    /// </summary>
    private void Apply(
        DependencyObject element,
        DependencyProperty property,
        DependencyProperty originalProperty,
        string current,
        Action<string> setter)
    {
        if (string.IsNullOrWhiteSpace(current) || IsBound(element, property))
        {
            return;
        }

        var original = element.GetValue(originalProperty) as string;
        if (string.IsNullOrWhiteSpace(original))
        {
            original = current;
            element.SetValue(originalProperty, original);
        }

        var translated = _localizationService.TranslateLiteral(original);
        if (!string.Equals(translated, current, StringComparison.Ordinal))
        {
            setter(translated);
        }
    }

    /// <summary>
    /// Reports whether the property is driven by a binding or a DynamicResource. Writing a literal
    /// over either replaces a live value with a frozen one, which is how bound totals, names, and
    /// metrics stop updating.
    /// </summary>
    private static bool IsBound(DependencyObject element, DependencyProperty property)
    {
        if (BindingOperations.GetBindingBase(element, property) is not null)
        {
            return true;
        }

        // DynamicResource resolves to a ResourceReferenceExpression rather than a binding, and
        // ApplyCulture already swaps those resource values, so leave expression-driven properties
        // untouched. Literals authored inside a template are still translated.
        return DependencyPropertyHelper.GetValueSource(element, property).IsExpression;
    }
}
