using System.Collections.ObjectModel;

namespace BookStore.UI.Services;

/// <summary>
/// Provides transient shell notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets active notifications.
    /// </summary>
    ObservableCollection<NotificationMessage> Notifications { get; }

    /// <summary>
    /// Shows a notification.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="severity">The severity.</param>
    void Show(string title, string message, NotificationSeverity severity);

    /// <summary>
    /// Shows a notification for a configured duration.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="message">The message.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="duration">The display duration.</param>
    void Show(string title, string message, NotificationSeverity severity, TimeSpan duration);
}
