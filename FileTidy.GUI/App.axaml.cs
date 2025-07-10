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
    using FileTidy.GUI.Views;
    using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<IFileStatusService>(serviceProvider =>
            {
                var fileOperationStore = serviceProvider.GetRequiredService<IFileOperationStore>();
                
                return new FileStatusService(fileOperationStore);
            });
            services.AddSingleton<ISortReporter, GuiFileSortReporter>();
            services.AddSingleton<IFileOperationService, FileOperationService>();
            services.AddSingleton<IFileCategoryService, FileCategoryService>();
            services.AddSingleton<IFileOrganizerService, FileOrganizerService>();
            
            services.AddSingleton<IFolderService, FolderService>();
            services.AddSingleton<FolderTreeViewModel>();
            services.AddSingleton<FileListViewModel>();
            services.AddSingleton<SortOperationViewModel>();
            services.AddSingleton<NotificationViewModel>();
            services.AddSingleton<HomeViewModel>();

            var serviceProvider = services.BuildServiceProvider();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var homeViewModel = serviceProvider.GetRequiredService<HomeViewModel>();
                
                var rootViewModel = new RootViewModel(homeViewModel);
                
                desktop.MainWindow = new RootView
                {
                    DataContext = rootViewModel,
                    Title = "FileTidy"
                };
            }

            Current!.Name = "FileTidy";

            base.OnFrameworkInitializationCompleted();
        }
    }