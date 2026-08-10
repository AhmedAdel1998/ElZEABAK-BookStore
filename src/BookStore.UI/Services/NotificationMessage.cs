namespace BookStore.UI.Services;

/// <summary>
/// Represents a transient shell notification.
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets notification severity.
    /// </summary>
    public NotificationSeverity Severity { get; set; }
}
