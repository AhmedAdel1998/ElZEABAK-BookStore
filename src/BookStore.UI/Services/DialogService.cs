using System.Windows;
using BookStore.Application.Interfaces;
using BookStore.UI.Dialogs;

namespace BookStore.UI.Services;

/// <summary>
/// Provides basic WPF message, confirmation, and error dialogs.
/// </summary>
public class DialogService : IDialogService, IMessageDialogService, IConfirmationDialogService, IErrorDialogService
{
    private readonly ILocalizationService _localizationService;

    /// <summary>Initializes a new instance of the <see cref="DialogService"/> class.</summary>
    public DialogService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <inheritdoc />
    public Task<DialogResultModel> ShowAsync(DialogRequest request)
    {
        var buttons = ResolveButtons(request.Kind, request.Buttons);
        var icon = ResolveIcon(request.Kind);
        var result = Show(request.Message, request.Title, buttons, icon);
        return Task.FromResult(new DialogResultModel(result.ToString()));
    }

    /// <inheritdoc />
    public Task ShowInformationAsync(string title, string message)
    {
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowWarningAsync(string title, string message)
    {
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowSuccessAsync(string title, string message)
    {
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowMessageAsync(string title, string message)
    {
        return ShowInformationAsync(title, message);
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message)
    {
        return ShowConfirmationAsync(title, message);
    }

    /// <inheritdoc />
    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    /// <inheritdoc />
    public Task<bool> ShowQuestionAsync(string title, string message)
    {
        return ShowConfirmationAsync(title, message);
    }

    /// <inheritdoc />
    public Task ShowErrorAsync(string title, string message)
    {
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows a translated message box, owned by the active window and mirrored for right-to-left
    /// languages. Without the RTL options the button row and Arabic text stay left-aligned, and
    /// without an owner the dialog can surface behind the shell.
    /// </summary>
    private MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        var text = L(message);
        var caption = L(title);
        var application = System.Windows.Application.Current;

        // The themed dialog owns its own button labels, so they follow the application language
        // rather than the Windows locale, and it inherits the app's theme, font and flow direction.
        if (application is not null && application.Dispatcher.CheckAccess())
        {
            return ShowThemedDialog(text, caption, buttons, icon);
        }

        var options = _localizationService.IsRightToLeft
            ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
            : MessageBoxOptions.None;
        var defaultResult = buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : MessageBoxResult.OK;
        return MessageBox.Show(text, caption, buttons, icon, defaultResult, options);
    }

    /// <summary>
    /// Shows the in-app dialog and maps its outcome onto the framework result the callers expect.
    /// </summary>
    private MessageBoxResult ShowThemedDialog(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
    {
        var isConfirmation = buttons is MessageBoxButton.YesNo or MessageBoxButton.OKCancel;
        var window = new AppDialogWindow { Owner = ResolveOwner() };

        var (glyph, brushKey) = icon switch
        {
            MessageBoxImage.Warning => ("\uE7BA", "WarningBrush"),
            MessageBoxImage.Error => ("\uE783", "DangerBrush"),
            MessageBoxImage.Question => ("\uE9CE", "AccentBrush"),
            _ => ("\uE946", "InfoBrush")
        };

        var primary = buttons == MessageBoxButton.YesNo ? L("Yes") : L("OK");
        var secondary = buttons switch
        {
            MessageBoxButton.YesNo => L("No"),
            MessageBoxButton.OKCancel => L("Cancel"),
            _ => null
        };

        window.Configure(caption, text, glyph, brushKey, primary, secondary);
        window.ShowDialog();

        if (!isConfirmation)
        {
            return MessageBoxResult.OK;
        }

        if (buttons == MessageBoxButton.YesNo)
        {
            return window.Confirmed ? MessageBoxResult.Yes : MessageBoxResult.No;
        }

        return window.Confirmed ? MessageBoxResult.OK : MessageBoxResult.Cancel;
    }

    private static Window? ResolveOwner()
    {
        var application = System.Windows.Application.Current;
        if (application is null || !application.Dispatcher.CheckAccess())
        {
            return null;
        }

        return application.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
            ?? application.MainWindow;
    }

    private string L(string text) => _localizationService.TranslateLiteral(text);

    private static MessageBoxButton ResolveButtons(DialogKind kind, IReadOnlyList<DialogButtonModel> buttons)
    {
        if (kind is DialogKind.Confirmation or DialogKind.Question)
        {
            return MessageBoxButton.YesNo;
        }

        return buttons.Count > 1 ? MessageBoxButton.OKCancel : MessageBoxButton.OK;
    }

    private static MessageBoxImage ResolveIcon(DialogKind kind) => kind switch
    {
        DialogKind.Warning => MessageBoxImage.Warning,
        DialogKind.Error => MessageBoxImage.Error,
        DialogKind.Confirmation or DialogKind.Question => MessageBoxImage.Question,
        _ => MessageBoxImage.Information
    };
}
