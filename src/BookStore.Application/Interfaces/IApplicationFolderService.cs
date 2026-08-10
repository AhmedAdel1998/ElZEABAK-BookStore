namespace BookStore.Application.Interfaces;

/// <summary>
/// Creates and verifies application runtime folders.
/// </summary>
public interface IApplicationFolderService
{
    /// <summary>
    /// Ensures all required runtime folders exist.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task EnsureRequiredFoldersAsync(CancellationToken cancellationToken = default);
}
