namespace BookStore.Shared.Models;

/// <summary>
/// Represents backup settings.
/// </summary>
public class BackupSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether backups are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the backup folder.
    /// </summary>
    public string Folder { get; set; } = "Backups";
}
