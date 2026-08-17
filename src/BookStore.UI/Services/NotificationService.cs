using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace BookStore.UI.Services;

/// <summary>
/// WPF notification service for transient shell messages.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILocalizationService _localizationService;

    /// <summary>Initializes a new instance of the <see cref="NotificationService"/> class.</summary>
    public NotificationService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <inheritdoc />
    public ObservableCollection<NotificationMessage> Notifications { get; } = [];

    /// <inheritdoc />
    public void Show(string title, string message, NotificationSeverity severity)
    {
        Show(title, message, severity, TimeSpan.FromSeconds(5));
    }

    /// <inheritdoc />
    public void Show(string title, string message, NotificationSeverity severity, TimeSpan duration)
    {
        var notification = new NotificationMessage
        {
            Title = _localizationService.TranslateLiteral(title),
            Message = _localizationService.TranslateLiteral(message),
            Severity = severity
        };
        Notifications.Add(notification);

        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Notifications.Remove(notification);
        };
        timer.Start();
    }

    /// <inheritdoc />
    public void Clear()
    {
        Notifications.Clear();
    }
}
