using System.Windows.Controls;
using BookStore.UI.Services;

namespace BookStore.UI.Views.Reports;

/// <summary>
/// Hosts a report's rows in an automatically generated grid.
/// </summary>
public partial class ReportGridView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="ReportGridView"/> class.</summary>
    public ReportGridView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Turns the generated column header into readable, translated text.
    /// </summary>
    /// <remarks>
    /// Auto-generated columns take their header from the DTO property name, so every report showed
    /// raw identifiers such as <c>QuantitySold</c> and <c>AverageSellingPrice</c> - in English, on all
    /// twelve reports. One handler covers them all without declaring columns twelve times over.
    /// </remarks>
    private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        var readable = ValidationLocalization.Humanize(e.PropertyName);
        e.Column.Header = LocalizationHelper.Translate(readable);

        // Money and counts read better right-aligned and consistently formatted.
        if (e.Column is DataGridTextColumn column && e.PropertyType is not null)
        {
            var type = Nullable.GetUnderlyingType(e.PropertyType) ?? e.PropertyType;
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            {
                column.Binding.StringFormat = "N2";
            }
            else if (type == typeof(DateTimeOffset) || type == typeof(DateTime))
            {
                column.Binding.StringFormat = "yyyy-MM-dd HH:mm";
            }
        }

        e.Column.MinWidth = 110;
    }
}
