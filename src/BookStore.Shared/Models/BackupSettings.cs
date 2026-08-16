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
    public string Folder { get; set; } = "%ProgramData%\\BookStore\\Backups";

    /// <summary>
    /// Gets or sets the temporary folder used for restore staging.
    /// </summary>
    public string TempFolder { get; set; } = "%ProgramData%\\BookStore\\Temp";

    /// <summary>
    /// Gets or sets the automatic backup frequency: Disabled, Daily, or Weekly.
    /// </summary>
    public string Frequency { get; set; } = "Daily";

    /// <summary>
    /// Gets or sets the number of non-pre-restore backups to retain.
    /// </summary>
    public int RetentionCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether backups are validated after creation.
    /// </summary>
    public bool ValidateAfterBackup { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether restore creates a pre-restore backup first.
    /// </summary>
    public bool CreatePreRestoreBackup { get; set; } = true;
}
