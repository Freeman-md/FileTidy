using System;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Services;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Reporting;

namespace FileTidy.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IFolderService _folderService;
    private readonly IFileOperationStore _fileOperationStore;
    private readonly IFileOperationLookupService _fileOperationLookupService;
    private readonly ISortReporter _sortReporter;
    private readonly IFileTidyingService _fileTidyingService;
    
    public FolderTreeViewModel FolderTreeViewModel { get; }
    public FileListViewModel FileListViewModel { get; }
    public SortOperationViewModel SortOperationViewModel { get; }
    public NotificationViewModel NotificationViewModel { get; }
    
    public string AppVersion => $"FileTidy v{Assembly.GetExecutingAssembly().GetName().Version} | Built by Freemancodz";
    
    public string SelectedFolderPath => FolderTreeViewModel.SelectedFolder?.FullPath ?? string.Empty;
    public bool ShouldShowEmptyState => !FileListViewModel.IsLoadingFiles && FolderTreeViewModel.SelectedFolder is null;
    public bool ShouldShowFileTable => !FileListViewModel.IsLoadingFiles && FolderTreeViewModel.SelectedFolder is not null;

    public MainViewModel() { }

    public MainViewModel(
        IFolderService folderService, 
        IFileOperationStore fileOperationStore, 
        IFileOperationLookupService fileOperationLookupService,
        FolderTreeViewModel folderTreeViewModel,
        FileListViewModel fileListViewModel,
        SortOperationViewModel sortOperationViewModel,
        NotificationViewModel notificationViewModel,
        ISortReporter sortReporter,
        IFileTidyingService fileTidyingService
    )
    {
        _folderService = folderService;
        _fileOperationStore = fileOperationStore;
        _fileOperationLookupService = fileOperationLookupService;
        _sortReporter = sortReporter;
        _fileTidyingService = fileTidyingService;

        FolderTreeViewModel = folderTreeViewModel;
        FileListViewModel = fileListViewModel;
        SortOperationViewModel = sortOperationViewModel;
        NotificationViewModel = notificationViewModel;

        // Subscribe to sortReporter events
        _sortReporter.ProgressChanged += progress => SortOperationViewModel.OperationProgress = progress;
        _sortReporter.ElapsedChanged += elapsed => SortOperationViewModel.ElapsedTime = elapsed;
        _sortReporter.FilesProcessedChanged += count => SortOperationViewModel.FilesProcessed = count;
        _sortReporter.NotificationRequested += (title, message) => NotificationViewModel.Show(title, message);

        FolderTreeViewModel.PropertyChanged += (sender, propertyChangedArgs) =>
        {
            if (propertyChangedArgs.PropertyName == nameof(FolderTreeViewModel.SelectedFolder))
            {
                OnPropertyChanged(nameof(SelectedFolderPath));
                OnPropertyChanged(nameof(ShouldShowEmptyState));
                OnPropertyChanged(nameof(ShouldShowFileTable));
                SortOperationViewModel.StartTidyingCommand.NotifyCanExecuteChanged();
            }
        };

        FileListViewModel.PropertyChanged += (sender, propertyChangedArgs) =>
        {
            if (propertyChangedArgs.PropertyName == nameof(FileListViewModel.IsLoadingFiles))
            {
                OnPropertyChanged(nameof(ShouldShowEmptyState));
                OnPropertyChanged(nameof(ShouldShowFileTable));
            }
        };

        FolderTreeViewModel.SelectedFolderChanged += (folder) =>
        {
            _ = FileListViewModel.LoadFilesForSelectedFolder();
        };

        _ = FolderTreeViewModel.InitializeAsync();
        _ = InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        var sessionIdStr = await _fileOperationStore.GetConfigValueAsync(nameof(SortOperationViewModel.LastSortSessionId));
        if (Guid.TryParse(sessionIdStr, out var restoredId))
            SortOperationViewModel.LastSortSessionId = restoredId;
    }

    // TODO: Preserve folder tree state (expanded nodes, selected folder) after sorting
    // - Before refreshing FolderTree, store expanded paths and selected folder path
    // - Refresh FolderTree in-place or rebuild while restoring expansion/selection
    // - Ensure UI updates and bindings stay intact after refresh
}
