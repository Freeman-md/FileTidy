    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using FileTidy.GUI.Contracts;
    using FileTidy.GUI.Services;
    using FileTidy.GUI.ViewModels;
    using Microsoft.Extensions.DependencyInjection;
    using MainViewModel = FileTidy.GUI.ViewModels.MainViewModel;

    namespace FileTidy.GUI;

    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IFolderService, FolderService>();
            services.AddSingleton<MainViewModel>();

            var serviceProvider = services.BuildServiceProvider();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Views.MainView
                {
                    DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }