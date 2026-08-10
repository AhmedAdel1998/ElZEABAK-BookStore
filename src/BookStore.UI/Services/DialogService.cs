using System.Windows;
using BookStore.Application.Interfaces;
using BookStore.UI.Dialogs;

namespace BookStore.UI.Services;

/// <summary>
/// Provides basic WPF message, confirmation, and error dialogs.
/// </summary>
public class DialogService : IDialogService, IMessageDialogService, IConfirmationDialogService, IErrorDialogService
{
    /// <inheritdoc />
    public Task<DialogResultModel> ShowAsync(DialogRequest request)
    {
        var buttons = ResolveButtons(request.Kind, request.Buttons);
        var icon = ResolveIcon(request.Kind);
        var result = MessageBox.Show(request.Message, request.Title, buttons, icon);
        return Task.FromResult(new DialogResultModel(result.ToString()));
    }

    /// <inheritdoc />
    public Task ShowInformationAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowWarningAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowSuccessAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
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
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
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
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

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
