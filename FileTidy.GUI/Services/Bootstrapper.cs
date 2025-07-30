using FileTidy.Core.Interfaces;
using FileTidy.Core.Services;
using FileTidy.Data.Sqlite;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Reporting;
using FileTidy.GUI.ViewModels.Home;
using FileTidy.GUI.ViewModels.Layouts;
using FileTidy.GUI.ViewModels.Onboarding;
using FileTidy.GUI.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace FileTidy.GUI.Services;

public class Bootstrapper
{
    public static ServiceProvider Init()
    {
        var services = new ServiceCollection();

        // Shared Services
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
        
        // Layout + Pages
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<OnboardingViewModel>();
        
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<HelpViewModel>();
        
        // Sub View-Models
        services.AddSingleton<FolderTreeViewModel>();
        services.AddSingleton<FileListViewModel>();
        services.AddSingleton<SortOperationViewModel>();
        services.AddSingleton<NotificationViewModel>();

        return services.BuildServiceProvider();
    }
}