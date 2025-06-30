    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using FileTidy.Core.Interfaces;
    using FileTidy.Core.Services;
    using FileTidy.Data.Sqlite;
    using FileTidy.GUI.Contracts;
using FileTidy.GUI.Reporting;
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

            services.AddSingleton<IFileOperationStore, SqliteOperationStore>();
            services.AddSingleton<IFileOperationLookupService>(serviceProvider =>
            {
                var fileOperationStore = serviceProvider.GetRequiredService<IFileOperationStore>();
                
                return new FileOperationLookupService(fileOperationStore);
            });
            services.AddSingleton<IFolderService, FolderService>();
            services.AddSingleton<ISortReporter, GuiFileSortReporter>();
            services.AddSingleton<IFileTidyingService, FileTidyingService>();
            
            services.AddSingleton<FolderTreeViewModel>();
            services.AddSingleton<FileListViewModel>();
            services.AddSingleton<SortOperationViewModel>();
            services.AddSingleton<NotificationViewModel>();
            services.AddSingleton<MainViewModel>();

            var serviceProvider = services.BuildServiceProvider();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Views.MainView
                {
                    DataContext = serviceProvider.GetRequiredService<MainViewModel>(),
                    Title = "FileTidy"
                };
            }

            Current!.Name = "FileTidy";

            base.OnFrameworkInitializationCompleted();
        }
    }