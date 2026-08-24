using BookStore.Application.Features.Backup.Services;
using BookStore.Shared.Constants;
using BookStore.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.Backup.Services;

public sealed class AutomaticBackupHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutomaticBackupHostedService> _logger;

    public AutomaticBackupHostedService(IServiceProvider serviceProvider, ILogger<AutomaticBackupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            using var scope = _serviceProvider.CreateScope();
            CleanupAbandonedRestoreFiles(scope.ServiceProvider);
            var automaticBackupService = scope.ServiceProvider.GetRequiredService<IAutomaticBackupService>();
            await automaticBackupService.RunIfDueAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automatic backup check failed.");
        }
    }

    private void CleanupAbandonedRestoreFiles(IServiceProvider serviceProvider)
    {
        try
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ApplicationSettings>>().Value.Backup;
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var tempFolder = (settings.TempFolder ?? "%ProgramData%\\BookStore\\Temp")
                .Replace("%ProgramData%", programData, StringComparison.OrdinalIgnoreCase);
            // A relative configured path used to land in the read-only install directory.
            tempFolder = ApplicationPaths.ResolveDataPath(tempFolder);
            if (!Directory.Exists(tempFolder))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(tempFolder, "bookstore-restore-*.dbtmp").Concat(Directory.EnumerateFiles(tempFolder, "bookstore-rollback-*.dbtmp")))
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Backup temporary file cleanup failed.");
        }
    }
}
