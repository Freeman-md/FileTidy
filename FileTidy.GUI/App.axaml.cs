using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FileTidy.GUI.Constants;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Helpers;
using FileTidy.GUI.Services;
using FileTidy.GUI.ViewModels.Layouts;
using FileTidy.GUI.Views;
using FileTidy.GUI.Views.Onboarding;
using Microsoft.Extensions.DependencyInjection;

namespace FileTidy.GUI;

public partial class App : Application
{
    public static readonly ServiceProvider Services = Bootstrapper.Init();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        HookGlobalErrorHandlers();

        var config = Services.GetRequiredService<IAppConfigService>();
        bool hasCompletedOnboarding = config.GetHasCompletedOnboardingAsync().GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = hasCompletedOnboarding
                ? new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainWindowViewModel>(),
                    Title = "FileTidy"
                }
                : new OnboardingWindow
                {
                    DataContext = Services.GetRequiredService<OnboardingWindowViewModel>()
                };
        }

        Current!.Name = "FileTidy";

        _ = Telemetry.LogAsync(TelemetryEventTypes.AppOpen, new { version = SystemInfoHelper.GetAppVersion() });

        base.OnFrameworkInitializationCompleted();
    }

    private static void HookGlobalErrorHandlers()
    {
        // 1) UI thread exceptions
        Dispatcher.UIThread.UnhandledException += async (_, e) =>
        {
            try
            {
                var message = TelemetrySanitizer.StripPaths(e.Exception.Message);
                var type    = e.Exception.GetType().Name;

                _ = Telemetry.LogAsync(TelemetryEventTypes.GlobalError, new
                {
                    kind = "ui",
                    exception = type,
                    message
                });

                // Allow app to keep running
                e.Handled = true;
            }
            catch { /* swallow */ }
        };

        // 2) Task exceptions
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            try
            {
                var ex = e.Exception.Flatten().InnerException ?? e.Exception;
                var message = TelemetrySanitizer.StripPaths(ex.Message);
                var type    = ex.GetType().Name;

                _ = Telemetry.LogAsync(TelemetryEventTypes.GlobalError, new
                {
                    kind = "task",
                    exception = type,
                    message
                });

                e.SetObserved();
            }
            catch { }
        };

        // 3) Domain-level crashes
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                var message = ex is null ? e.ExceptionObject?.ToString() ?? "unknown"
                                         : TelemetrySanitizer.StripPaths(ex.Message);
                var type    = ex?.GetType().Name ?? "Unknown";

                _ = Telemetry.LogAsync(TelemetryEventTypes.AppCrash, new
                {
                    kind = "domain",
                    exception = type,
                    message,
                    isTerminating = e.IsTerminating
                });
            }
            catch { }
        };
    }

    public static void LaunchMainWindow()
    {
        if (Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();
            var newMainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            // Show new window first
            newMainWindow.Show();

            // Close old onboarding window
            (desktop.MainWindow as Window)?.Close();

            // Swap reference
            desktop.MainWindow = newMainWindow;
        }
    }
}
