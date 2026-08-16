using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Configuration;

/// <summary>
/// Creates required application runtime folders.
/// </summary>
public class ApplicationFolderService : IApplicationFolderService
{
    private readonly ILogger<ApplicationFolderService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationFolderService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ApplicationFolderService(ILogger<ApplicationFolderService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task EnsureRequiredFoldersAsync(CancellationToken cancellationToken = default)
    {
        string[] folders =
        [
            FolderConstants.Database,
            FolderConstants.Logs,
            FolderConstants.Backups,
            FolderConstants.Exports,
            FolderConstants.Temp
        ];

        foreach (var folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(ApplicationPaths.ResolveDataPath(folder));
            _logger.LogInformation("Runtime folder verified: {Folder}", folder);
        }

        return Task.CompletedTask;
    }
}
