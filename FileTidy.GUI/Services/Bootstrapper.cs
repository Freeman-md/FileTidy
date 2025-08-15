using System;
using System.IO;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Services;
using FileTidy.Data.Sqlite;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Reporting;
using FileTidy.GUI.Services;
using FileTidy.GUI.ViewModels.Home;
using FileTidy.GUI.ViewModels.Layouts;
using FileTidy.GUI.ViewModels.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnboardingWindowViewModel = FileTidy.GUI.ViewModels.Layouts.OnboardingWindowViewModel;

namespace FileTidy.GUI.Services;

public static class Bootstrapper
{
    public static ServiceProvider Init()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpClient();

        // Shared services
        services.AddSingleton<IDeviceTelemetryService, DeviceTelemetryService>();
        services.AddSingleton<IAppConfigService, AppConfigService>();
        services.AddSingleton<IFileOperationStore, SqliteOperationStore>();
        services.AddSingleton<IFileStatusService>(sp =>
        {
            var fileOperationStore = sp.GetRequiredService<IFileOperationStore>();
            return new FileStatusService(fileOperationStore);
        });
        services.AddSingleton<ISortReporter, GuiFileSortReporter>();
        services.AddSingleton<IFileOperationService, FileOperationService>();
        services.AddSingleton<IFileCategoryService, FileCategoryService>();
        services.AddSingleton<IFileOrganizerService, FileOrganizerService>();
        services.AddSingleton<IFolderService, FolderService>();

        // Layout + Pages
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<OnboardingWindowViewModel>();

        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<HelpViewModel>();

        // Sub ViewModels
        services.AddSingleton<FolderTreeViewModel>();
        services.AddSingleton<FileListViewModel>();
        services.AddSingleton<SortOperationViewModel>();
        services.AddSingleton<NotificationViewModel>();

        return services.BuildServiceProvider();
    }
}
