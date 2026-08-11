using System.Windows;
using System.Windows.Controls;
using BookStore.Application.Features.Reports.DTOs;

namespace BookStore.UI.Controls;

public partial class ReportDateRangeControl : UserControl
{
    public static readonly DependencyProperty SelectedPresetProperty = DependencyProperty.Register(
        nameof(SelectedPreset),
        typeof(ReportDateRangePreset),
        typeof(ReportDateRangeControl),
        new FrameworkPropertyMetadata(ReportDateRangePreset.Today, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPresetChanged));

    public static readonly DependencyProperty StartDateProperty = DependencyProperty.Register(
        nameof(StartDate),
        typeof(DateTime?),
        typeof(ReportDateRangeControl),
        new FrameworkPropertyMetadata(DateTime.Today, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty EndDateProperty = DependencyProperty.Register(
        nameof(EndDate),
        typeof(DateTime?),
        typeof(ReportDateRangeControl),
        new FrameworkPropertyMetadata(DateTime.Today, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ReportDateRangeControl()
    {
        InitializeComponent();
    }

    public ReportDateRangePreset SelectedPreset
    {
        get => (ReportDateRangePreset)GetValue(SelectedPresetProperty);
        set => SetValue(SelectedPresetProperty, value);
    }

    public DateTime? StartDate
    {
        get => (DateTime?)GetValue(StartDateProperty);
        set => SetValue(StartDateProperty, value);
    }

    public DateTime? EndDate
    {
        get => (DateTime?)GetValue(EndDateProperty);
        set => SetValue(EndDateProperty, value);
    }

    private static void OnPresetChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not ReportDateRangeControl control || args.NewValue is not ReportDateRangePreset preset || preset == ReportDateRangePreset.Custom)
        {
            return;
        }

        var range = ReportDateRange.FromPreset(preset);
        control.StartDate = range.StartDate.LocalDateTime.Date;
        control.EndDate = range.EndDate.AddTicks(-1).LocalDateTime.Date;
    }
}
