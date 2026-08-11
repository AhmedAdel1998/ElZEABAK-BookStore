namespace BookStore.Application.Features.Backup.DTOs;

public enum BackupType
{
    Manual,
    Automatic,
    PreRestore
}

public enum BackupValidationStatus
{
    Unknown,
    Valid,
    Invalid
}

public enum BackupFrequency
{
    Disabled,
    Daily,
    Weekly
}

public sealed class BackupMetadataDto
{
    public Guid BackupId { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public string DatabaseVersion { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public BackupType BackupType { get; set; }
    public BackupValidationStatus ValidationStatus { get; set; } = BackupValidationStatus.Unknown;
    public string ChecksumSha256 { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string MachineName { get; set; } = Environment.MachineName;
    public string? ValidationMessage { get; set; }
}

public sealed class BackupOperationResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public BackupMetadataDto? Backup { get; init; }
    public bool RestartRequired { get; init; }

    public static BackupOperationResult Success(string message, BackupMetadataDto? backup = null, bool restartRequired = false) => new()
    {
        Succeeded = true,
        Message = message,
        Backup = backup,
        RestartRequired = restartRequired
    };

    public static BackupOperationResult Failure(string message, BackupMetadataDto? backup = null) => new()
    {
        Succeeded = false,
        Message = message,
        Backup = backup
    };
}

public sealed class DatabaseIntegrityResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DatabasePath { get; set; } = string.Empty;
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan Duration { get; set; }
}

public sealed class DatabaseHealthResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DatabasePath { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public long AvailableDiskSpaceBytes { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
}
