
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using FileTidy.GUI.Services;
    using FileTidy.GUI.ViewModels.Layouts;
    using FileTidy.GUI.Views;
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
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainWindowViewModel>(),
                    Title = "FileTidy"
                };
            }

            Current!.Name = "FileTidy";

            base.OnFrameworkInitializationCompleted();
        }
    }