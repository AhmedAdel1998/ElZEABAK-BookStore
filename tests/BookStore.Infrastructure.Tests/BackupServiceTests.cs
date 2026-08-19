using BookStore.Application.Features.Authentication.Responses;
using BookStore.Shared.Results;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Backup.DTOs;
using BookStore.Application.Features.Backup.Services;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Persistence.Context;
using BookStore.Infrastructure.Backup.Services;
using BookStore.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Tests;

public sealed class BackupServiceTests
{
    [Fact]
    public async Task CreateBackup_CreatesValidatedMetadataAndChecksum()
    {
        await using var fixture = await BackupFixture.CreateAsync();

        var result = await fixture.BackupService.CreateBackupAsync(BackupType.Manual);
        var backups = await fixture.BackupService.ListBackupsAsync();

        Assert.True(result.Succeeded, result.Message);
        Assert.NotNull(result.Backup);
        Assert.True(File.Exists(result.Backup.FilePath));
        Assert.True(File.Exists(result.Backup.FilePath + ".meta.json"));
        Assert.Equal(BackupValidationStatus.Valid, result.Backup.ValidationStatus);
        Assert.Equal(64, result.Backup.ChecksumSha256.Length);
        Assert.Single(backups);
    }

    [Fact]
    public async Task ValidateBackup_RejectsCorruptedBackupByChecksum()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        var created = await fixture.BackupService.CreateBackupAsync(BackupType.Manual);
        await CorruptFileAsync(created.Backup!.FilePath);

        var validation = await fixture.BackupService.ValidateBackupAsync(created.Backup.BackupId);

        Assert.False(validation.Succeeded);
        Assert.Equal(BackupValidationStatus.Invalid, validation.Backup!.ValidationStatus);
    }

    [Fact]
    public async Task RestoreBackup_RestoresDataAndCreatesPreRestoreBackup()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        var created = await fixture.BackupService.CreateBackupAsync(BackupType.Manual);
        await fixture.AddCategoryAsync("After Backup");

        var restore = await fixture.BackupService.RestoreBackupAsync(created.Backup!.BackupId, BackupFixture.ConfirmationText);
        var categoryNames = await fixture.GetCategoryNamesAsync();
        var backups = await fixture.BackupService.ListBackupsAsync();

        Assert.True(restore.Succeeded, restore.Message);
        Assert.True(restore.RestartRequired);
        Assert.Contains("Before Backup", categoryNames);
        Assert.DoesNotContain("After Backup", categoryNames);
        Assert.Contains(backups, backup => backup.BackupType == BackupType.PreRestore && backup.ValidationStatus == BackupValidationStatus.Valid);
    }

    [Fact]
    public async Task RestoreBackup_WithInvalidBackupLeavesProductionDatabaseUntouched()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        var created = await fixture.BackupService.CreateBackupAsync(BackupType.Manual);
        await fixture.AddCategoryAsync("After Backup");
        await CorruptFileAsync(created.Backup!.FilePath);

        var restore = await fixture.BackupService.RestoreBackupAsync(created.Backup.BackupId, BackupFixture.ConfirmationText);
        var categoryNames = await fixture.GetCategoryNamesAsync();

        Assert.False(restore.Succeeded);
        Assert.Contains("Before Backup", categoryNames);
        Assert.Contains("After Backup", categoryNames);
    }

    [Fact]
    public async Task CreateBackup_WhenDiskSpaceIsInsufficient_DoesNotStart()
    {
        await using var fixture = await BackupFixture.CreateAsync(hasEnoughSpace: false);

        var result = await fixture.BackupService.CreateBackupAsync(BackupType.Manual);

        Assert.False(result.Succeeded);
        Assert.Empty(await fixture.BackupService.ListBackupsAsync());
    }

    [Fact]
    public async Task CleanupBackups_AppliesRetentionAndKeepsValidRecoveryPoint()
    {
        await using var fixture = await BackupFixture.CreateAsync(retentionCount: 2);
        for (var index = 0; index < 4; index++)
        {
            await fixture.BackupService.CreateBackupAsync(BackupType.Manual);
        }

        var cleanup = await fixture.BackupService.CleanupBackupsAsync();
        var backups = await fixture.BackupService.ListBackupsAsync();

        Assert.True(cleanup.Succeeded);
        Assert.True(backups.Count <= 2);
        Assert.Contains(backups, backup => backup.ValidationStatus == BackupValidationStatus.Valid);
    }

    [Fact]
    public async Task DatabaseHealth_ReportsHealthyTemporaryDatabase()
    {
        await using var fixture = await BackupFixture.CreateAsync();

        var health = await fixture.IntegrityService.CheckHealthAsync();

        Assert.True(health.IsHealthy);
        Assert.Contains("passed", health.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(health.AvailableDiskSpaceBytes > 0);
    }

    private sealed class BackupFixture : IAsyncDisposable
    {
        public const string ConfirmationText = "I understand that restoring will replace the current database.";

        private BackupFixture(string root, string databasePath, BackupService backupService, DatabaseIntegrityService integrityService)
        {
            Root = root;
            DatabasePath = databasePath;
            BackupService = backupService;
            IntegrityService = integrityService;
        }

        public string Root { get; }
        public string DatabasePath { get; }
        public BackupService BackupService { get; }
        public DatabaseIntegrityService IntegrityService { get; }

        public static async Task<BackupFixture> CreateAsync(bool hasEnoughSpace = true, int retentionCount = 10)
        {
            var root = Path.Combine(Path.GetTempPath(), "bookstore-backup-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var databasePath = Path.Combine(root, "bookstore-test.db");
            await using (var context = CreateContext(databasePath))
            {
                await context.Database.EnsureCreatedAsync();
                context.Categories.Add(new Category("Before Backup"));
                await context.SaveChangesAsync();
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:BookStoreDb"] = $"Data Source={databasePath}"
                })
                .Build();
            var settings = Options.Create(new ApplicationSettings
            {
                Backup = new BackupSettings
                {
                    Folder = Path.Combine(root, "Backups"),
                    TempFolder = Path.Combine(root, "Temp"),
                    RetentionCount = retentionCount,
                    CreatePreRestoreBackup = true
                }
            });
            var resolver = new DatabasePathResolver(configuration);
            var disk = new FakeDiskSpaceService(hasEnoughSpace);
            var currentUser = new FakeCurrentUserService();
            // Mirrors the configured values, so these tests exercise the stored-settings path that
            // production now uses while keeping their original intent.
            var storedSettings = new StubSettingsService(new BackupSettingsDto
            {
                BackupLocation = Path.Combine(root, "Backups"),
                RetentionCount = retentionCount,
                CreatePreRestoreBackup = true
            });
            var backupService = new BackupService(resolver, disk, currentUser, settings, storedSettings, NullLogger<BackupService>.Instance);
            var integrityService = new DatabaseIntegrityService(resolver, disk, NullLogger<DatabaseIntegrityService>.Instance);
            return new BackupFixture(root, databasePath, backupService, integrityService);
        }

        public async Task AddCategoryAsync(string name)
        {
            await using var context = CreateContext(DatabasePath);
            context.Categories.Add(new Category(name));
            await context.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<string>> GetCategoryNamesAsync()
        {
            await using var context = CreateContext(DatabasePath);
            return await context.Categories.IgnoreQueryFilters().Select(category => category.Name).OrderBy(name => name).ToArrayAsync();
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
            }

            return ValueTask.CompletedTask;
        }

        private static BookStoreDbContext CreateContext(string databasePath)
        {
            var options = new DbContextOptionsBuilder<BookStoreDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            return new BookStoreDbContext(options);
        }
    }

    private static async Task CorruptFileAsync(string filePath)
    {
        await using var stream = new FileStream(filePath, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync("not a sqlite database");
    }

    private sealed class StubSettingsService(BackupSettingsDto backup) : ISettingsService
    {
        public Task<T> GetAsync<T>(CancellationToken cancellationToken = default)
            where T : class, new()
        {
            object value = typeof(T) == typeof(BackupSettingsDto) ? backup : new T();
            return Task.FromResult((T)value);
        }

        public Task<Result> SetAsync<T>(T settings, CancellationToken cancellationToken = default)
            where T : class, new() => Task.FromResult(Result.Success());

        public Task<IReadOnlyList<SettingEntryDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SettingEntryDto>>([]);
        public Task<Result> ResetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task<Result> ResetCategoryAsync(SettingsCategory category, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeDiskSpaceService(bool hasEnoughSpace) : IDiskSpaceService
    {
        public bool HasEnoughSpace(string folderPath, long requiredBytes) => hasEnoughSpace;
        public long GetAvailableBytes(string folderPath) => hasEnoughSpace ? long.MaxValue / 2 : 0;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public string? Username => "admin";
        public string? FullName => "Administrator";
        public string? Role => "Administrator";
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? SessionId => Guid.NewGuid();
        public DateTimeOffset? LoginTime => DateTimeOffset.UtcNow;
        public void SignIn(UserSessionSnapshot session) { }
        public void SignOut() { }
    }
}
