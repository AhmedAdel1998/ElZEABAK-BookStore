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
using BookStore.UI.Dialogs;
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

            _logger.LogInformation("Application start");

            await _host.Services.GetRequiredService<IApplicationFolderService>().EnsureRequiredFoldersAsync();
            await _host.Services.GetRequiredService<ILocalizationService>().ApplyConfiguredCultureAsync();
            await _host.Services.GetRequiredService<IThemeService>().ApplyConfiguredThemeAsync();

            // CurrencyValueConverter is instantiated by XAML with no constructor, so it cannot
            // receive this service through DI; assigning the resolved singleton here is what lets
            // every {StaticResource CurrencyConverter} binding read the configured currency.
            var currencyFormatter = _host.Services.GetRequiredService<ICurrencyFormatterService>();
            await currencyFormatter.ApplyConfiguredCurrencyAsync();
            BookStore.UI.Converters.CurrencyValueConverter.FormatterService = currencyFormatter;
            var navigationService = _host.Services.GetRequiredService<INavigationService>();
            var firstRunSetupService = _host.Services.GetRequiredService<IFirstRunSetupService>();
            if (await firstRunSetupService.IsSetupRequiredAsync())
            {
                await navigationService.NavigateToAsync<FirstRunSetupViewModel>();
                var setupWindow = _host.Services.GetRequiredService<MainWindow>();
                setupWindow.Show();
                base.OnStartup(e);
                return;
            }

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
                var logFolder = ApplicationPaths.ResolveDataPath(FolderConstants.Logs);
                Directory.CreateDirectory(logFolder);
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.File(Path.Combine(logFolder, "bookstore-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<ApplicationSettings>(context.Configuration.GetSection("Application"));
                services.AddLogging(builder =>
                {
                    builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                    builder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
                });

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
        var logFolder = ApplicationPaths.ResolveDataPath(FolderConstants.Logs);
        Directory.CreateDirectory(logFolder);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logFolder, "bookstore-.log"), rollingInterval: RollingInterval.Day)
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
        var correlationId = NewCorrelationId();
        LogException(e.Exception, "Unhandled UI exception", correlationId);
        ShowFriendlyError(e.Exception, correlationId);
        e.Handled = true;
    }

    private void OnUnhandledAppDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        var correlationId = NewCorrelationId();
        if (e.ExceptionObject is Exception exception)
        {
            LogException(exception, "Unhandled AppDomain exception", correlationId);
            ShowFriendlyError(exception, correlationId);
        }
        else
        {
            // AppDomain.UnhandledExceptionEventArgs.ExceptionObject is documented as "usually" an
            // Exception, but is untyped, so a non-Exception payload (rare, but legal in .NET) must
            // still be logged and reported under its own correlation id rather than silently
            // dropped.
            Log.Error("Unhandled AppDomain exception with non-exception payload {Payload} (correlation {CorrelationId})", e.ExceptionObject, correlationId);
            ShowFriendlyError(exception: null, correlationId);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var correlationId = NewCorrelationId();
        LogException(e.Exception, "Unhandled task exception", correlationId);
        ShowFriendlyError(e.Exception, correlationId);
        e.SetObserved();
    }

    /// <summary>
    /// Generates a short identifier to join a user-facing crash report with its log entry. Full
    /// GUIDs are unreadable when a user has to read one aloud or retype it; eight hex characters
    /// keep collisions implausible for a single desktop install while staying easy to communicate.
    /// </summary>
    private static string NewCorrelationId() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private void LogException(Exception exception, string message, string correlationId)
    {
        _logger?.LogError(exception, "{Message} (correlation {CorrelationId})", message, correlationId);
        Log.Error(exception, "{Message} (correlation {CorrelationId})", message, correlationId);
    }

    /// <summary>
    /// Shows the crash dialog with the same correlation id the exception was just logged under, so
    /// a user report and its log entry can be matched without asking what the user was doing.
    /// </summary>
    private void ShowFriendlyError(Exception? exception, string correlationId)
    {
        var model = new CrashDialogModel(correlationId, BuildCrashDetails(exception, correlationId));

        void ShowDialog()
        {
            try
            {
                new CrashDialogWindow(model) { Owner = TryFindOwnerWindow() }.ShowDialog();
            }
            catch (Exception dialogException)
            {
                // The crash dialog itself failing to render (for example, no window session is
                // available yet) must not prevent the user from learning the operation failed.
                Log.Error(dialogException, "Failed to show the crash dialog (correlation {CorrelationId})", correlationId);
                ShowFallbackError(correlationId);
            }
        }

        // The three global handlers above can each fire from a background thread (a task's
        // finalizer thread for UnobservedTaskException in particular), and a WPF Window can only
        // be created and shown on the dispatcher thread that owns it.
        if (Dispatcher.CheckAccess())
        {
            ShowDialog();
        }
        else
        {
            Dispatcher.Invoke(ShowDialog);
        }
    }

    private static string BuildCrashDetails(Exception? exception, string correlationId)
    {
        var header = $"{ApplicationConstants.ApplicationName}{Environment.NewLine}" +
                     $"Correlation: {correlationId}{Environment.NewLine}" +
                     $"Time (UTC): {DateTimeOffset.UtcNow:O}";

        return exception is null ? header : $"{header}{Environment.NewLine}{Environment.NewLine}{exception}";
    }

    private Window? TryFindOwnerWindow() =>
        Windows.OfType<Window>().FirstOrDefault(window => window.IsActive) ?? MainWindow;

    /// <summary>
    /// Last-resort error surface used before the host (and therefore the WPF resource dictionaries
    /// the crash dialog depends on) has finished building, and if the crash dialog itself throws.
    /// </summary>
    private static void ShowFallbackError(string? correlationId = null)
    {
        var message = correlationId is null
            ? MessageConstants.FriendlyUnhandledException
            : $"{MessageConstants.FriendlyUnhandledException}{Environment.NewLine}Reference: {correlationId}";

        MessageBox.Show(
            message,
            ApplicationConstants.ApplicationName,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
