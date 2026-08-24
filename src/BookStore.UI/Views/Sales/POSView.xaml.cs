using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace BookStore.UI.Views.Sales;

/// <summary>
/// Interaction logic for the POS cashier view.
/// </summary>
/// <remarks>
/// A barcode scanner is a keyboard: it types the code into whatever has focus and presses Enter.
/// Focus drifts constantly at a till -- the cashier clicks a quick-cash button, selects a cart row,
/// picks a customer -- and a scan aimed at the barcode box then lands somewhere else, or nowhere at
/// all, because a clicked button swallows the keystrokes without showing them. This view therefore
/// does two things: it steers stray scanner input into the barcode box, and it returns focus there
/// after any button press.
/// </remarks>
public partial class POSView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="POSView"/> class.</summary>
    public POSView()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        // Every button on the page, rather than the handful that were wired up by hand. Registered
        // for handled events too, because a Command-bound button marks its click handled.
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnAnyButtonClicked), handledEventsToo: true);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => FocusBarcodeInput();

    /// <summary>
    /// Sends a keystroke that arrived while nothing was ready to receive it into the barcode box.
    /// </summary>
    /// <remarks>
    /// Preview text input tunnels from the window down, so this runs before the focused element
    /// sees the character. Fields the cashier is deliberately typing in are left alone; everything
    /// else -- buttons, cart rows, lists, the page itself -- hands its input to the barcode box.
    /// </remarks>
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        if (IsScannerInputAdrift(e.Text))
        {
            // A scan is a whole new code, so it replaces whatever the box held rather than
            // appending to a half-typed leftover.
            BarcodeInput.Focus();
            BarcodeInput.Text = e.Text;
            BarcodeInput.CaretIndex = BarcodeInput.Text.Length;

            // The rest of the burst, and the trailing Enter, now land in the box on their own.
            e.Handled = true;
            return;
        }

        base.OnPreviewTextInput(e);
    }

    /// <summary>Returns focus to the barcode box after a scan so the next one lands in the right place.</summary>
    private void OnBarcodeKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Return or Key.Enter)
        {
            FocusBarcodeInput();
        }
    }

    private void OnAnyButtonClicked(object sender, RoutedEventArgs e) => FocusBarcodeInput();

    private static bool IsScannerInputAdrift(string? text)
    {
        if (string.IsNullOrEmpty(text) || char.IsControl(text[0]))
        {
            return false;
        }

        return !IsTypingTarget(Keyboard.FocusedElement);
    }

    /// <summary>
    /// Reports whether the focused element is somewhere a person types on purpose. The barcode box
    /// itself counts, so its own keystrokes are never rewritten.
    /// </summary>
    private static bool IsTypingTarget(IInputElement? focused) => focused switch
    {
        TextBoxBase => true,
        PasswordBox => true,
        ComboBox { IsEditable: true } => true,
        _ => false
    };

    private void FocusBarcodeInput()
    {
        Dispatcher.BeginInvoke(
            () =>
            {
                // Never pull focus out of a dialog that opened in the meantime: completing a sale
                // asks for confirmation, and the owner window reports inactive while it is up.
                if (!IsVisible || !BarcodeInput.IsVisible || Window.GetWindow(this)?.IsActive != true)
                {
                    return;
                }

                BarcodeInput.Focus();
                BarcodeInput.SelectAll();
            },
            DispatcherPriority.Input);
    }
}
