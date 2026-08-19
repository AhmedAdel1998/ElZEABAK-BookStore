using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BookStore.UI.Views.Sales;

/// <summary>
/// Interaction logic for the POS cashier view.
/// </summary>
public partial class POSView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="POSView"/> class.</summary>
    public POSView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        FocusBarcodeInput();
    }

    private void OnBarcodeAddClicked(object sender, RoutedEventArgs e)
    {
        FocusBarcodeInput();
    }

    /// <summary>
    /// Returns focus to the barcode box after a scan so the next one lands in the right place.
    /// </summary>
    private void OnBarcodeKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Return or Key.Enter)
        {
            FocusBarcodeInput();
        }
    }

    private void OnCashierActionClicked(object sender, RoutedEventArgs e)
    {
        FocusBarcodeInput();
    }

    private void FocusBarcodeInput()
    {
        Dispatcher.BeginInvoke(() =>
        {
            BarcodeInput.Focus();
            BarcodeInput.SelectAll();
        }, DispatcherPriority.Input);
    }
}
