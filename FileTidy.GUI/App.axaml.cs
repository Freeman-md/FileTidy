    using System;
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
    using FileTidy.GUI.ViewModels.Home;
    using FileTidy.GUI.ViewModels.Onboarding;
    using FileTidy.GUI.Views;
    using Microsoft.Extensions.DependencyInjection;
    using FileListViewModel = FileTidy.GUI.ViewModels.Home.FileListViewModel;
    using FolderTreeViewModel = FileTidy.GUI.ViewModels.Home.FolderTreeViewModel;
    using NotificationViewModel = FileTidy.GUI.ViewModels.Home.NotificationViewModel;
    using SortOperationViewModel = FileTidy.GUI.ViewModels.Home.SortOperationViewModel;

    namespace FileTidy.GUI;

    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

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
            services.AddSingleton<OnboardingViewModel>();
            services.AddSingleton<RootViewModel>(serviceProvider =>
            {
                var onboardingViewModel = serviceProvider.GetRequiredService<OnboardingViewModel>();
                
                return new RootViewModel(onboardingViewModel);
            });
            services.AddSingleton<INavigationService>(serviceProvider =>
            {
                var rootViewModel = serviceProvider.GetRequiredService<RootViewModel>();
                
                return new NavigationService(rootViewModel);
            });
            services.AddSingleton<SettingsViewModel>(serviceProvider => 
            {
                var navigationService = serviceProvider.GetRequiredService<INavigationService>();
                var homeViewModel = serviceProvider.GetRequiredService<HomeViewModel>();
                
                return new SettingsViewModel(navigationService, homeViewModel);
            });
            services.AddSingleton<HelpViewModel>(serviceProvider => 
            {
                var navigationService = serviceProvider.GetRequiredService<INavigationService>();
                var homeViewModel = serviceProvider.GetRequiredService<HomeViewModel>();
                
                return new HelpViewModel(navigationService, homeViewModel);
            });
            
            Services = services.BuildServiceProvider();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new RootView
                {
                    DataContext = Services.GetRequiredService<RootViewModel>(),
                    Title = "FileTidy"
                };
            }

            Current!.Name = "FileTidy";

            base.OnFrameworkInitializationCompleted();
        }
    }