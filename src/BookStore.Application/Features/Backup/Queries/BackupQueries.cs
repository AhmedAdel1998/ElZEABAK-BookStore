namespace BookStore.Application.Features.Backup.Queries;

public sealed record GetBackupsQuery;

public sealed record GetBackupDetailsQuery(Guid BackupId);

public sealed record GetDatabaseHealthQuery;
