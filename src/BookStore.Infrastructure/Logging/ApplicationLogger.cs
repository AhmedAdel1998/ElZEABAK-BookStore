using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Logging;

/// <summary>
/// Central infrastructure logger wrapper.
/// </summary>
public class ApplicationLogger(ILogger<ApplicationLogger> logger)
{
    /// <summary>
    /// Gets the typed logger instance.
    /// </summary>
    protected ILogger<ApplicationLogger> Logger { get; } = logger;
}
