
    using System;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using FileTidy.GUI.Contracts;
    using FileTidy.GUI.Services;
    using FileTidy.GUI.ViewModels.Layouts;
    using FileTidy.GUI.Views;
    using FileTidy.GUI.Views.Onboarding;
    using Microsoft.Extensions.DependencyInjection;
    using OnboardingWindowViewModel = FileTidy.GUI.ViewModels.Layouts.OnboardingWindowViewModel;

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

            base.OnFrameworkInitializationCompleted();
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

                // Show the new window *first*
                newMainWindow.Show();

                // Then close the old onboarding window
                (desktop.MainWindow as Window)?.Close();

                // Reassign the main window
                desktop.MainWindow = newMainWindow;
            }
        }
    }