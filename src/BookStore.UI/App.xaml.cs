using System.Windows;
using System.Windows.Threading;
using System.IO;
using BookStore.Application.Interfaces;
using BookStore.Application;
using BookStore.Infrastructure;
using BookStore.Persistence;
using BookStore.Reporting;
using BookStore.Shared.Constants;
using BookStore.Shared.Models;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using BookStore.UI.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BookStore.UI;

/// <summary>
/// WPF application entry point and composition root.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IErrorDialogService? _errorDialogService;
    private Microsoft.Extensions.Logging.ILogger<App>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        ConfigureGlobalExceptionHandling();
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            ConfigureBootstrapLogger();

            _host = CreateHostBuilder(e.Args).Build();
            await _host.StartAsync();

            _logger = _host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>();
            _errorDialogService = _host.Services.GetRequiredService<IErrorDialogService>();

            _logger.LogInformation("Application start");

            await _host.Services.GetRequiredService<IApplicationFolderService>().EnsureRequiredFoldersAsync();
            await _host.Services.GetRequiredService<IThemeService>().ApplyConfiguredThemeAsync();
            var navigationService = _host.Services.GetRequiredService<INavigationService>();
            var authenticationService = _host.Services.GetRequiredService<IAuthenticationService>();
            var sessionTimeoutService = _host.Services.GetRequiredService<ISessionTimeoutService>();
            var rememberedSession = await authenticationService.TryRestoreRememberedSessionAsync();

            if (rememberedSession.Succeeded)
            {
                sessionTimeoutService.Start();
                await navigationService.NavigateToAsync<AuthenticatedHomeViewModel>();
            }
            else
            {
                await navigationService.NavigateToAsync<LoginViewModel>();
            }

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed during startup");
            ShowFallbackError();
            Shutdown(-1);
        }
    }

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _logger?.LogInformation("Application shutdown");
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>
    /// Creates the application host builder.
    /// </summary>
    /// <param name="args">Startup arguments.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.SetBasePath(AppContext.BaseDirectory);
                builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                builder.AddEnvironmentVariables(prefix: "BOOKSTORE_");
            })
            .UseSerilog((context, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext();
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<ApplicationSettings>(context.Configuration.GetSection("Application"));

                services
                    .AddApplication()
                    .AddPersistence(context.Configuration)
                    .AddInfrastructure()
                    .AddReporting()
                    .AddPresentation();
            });
    }

    private static void ConfigureBootstrapLogger()
    {
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, FolderConstants.Logs));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs", "bookstore-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    private void ConfigureGlobalExceptionHandling()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledAppDomainException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "Unhandled UI exception");
        ShowFriendlyError();
        e.Handled = true;
    }

    private void OnUnhandledAppDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogException(exception, "Unhandled AppDomain exception");
        }

        ShowFriendlyError();
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception, "Unhandled task exception");
        ShowFriendlyError();
        e.SetObserved();
    }

    private void LogException(Exception exception, string message)
    {
        _logger?.LogError(exception, "{Message}", message);
        Log.Error(exception, "{Message}", message);
    }

    private void ShowFriendlyError()
    {
        if (_errorDialogService is not null)
        {
            _ = _errorDialogService.ShowErrorAsync(ApplicationConstants.ApplicationName, MessageConstants.FriendlyUnhandledException);
            return;
        }

        ShowFallbackError();
    }

    private static void ShowFallbackError()
    {
        MessageBox.Show(
            MessageConstants.FriendlyUnhandledException,
            ApplicationConstants.ApplicationName,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
