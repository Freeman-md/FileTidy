using System;
using System.Threading.Tasks;
using FileTidy.Core.Interfaces;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.ViewModels.Home;

namespace FileTidy.GUI.ViewModels.Pages;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IFileOperationStore _fileOperationStore;
    private readonly ISortReporter _sortReporter;
    
    public FolderTreeViewModel FolderTreeViewModel { get; }
    public FileListViewModel FileListViewModel { get; }
    public SortOperationViewModel SortOperationViewModel { get; }
    public NotificationViewModel NotificationViewModel { get; }
    
    public string SelectedFolderPath => FolderTreeViewModel.SelectedFolder?.FullPath ?? string.Empty;
    public bool ShouldShowEmptyState => !FileListViewModel.IsLoadingFiles && FolderTreeViewModel.SelectedFolder is null;
    public bool ShouldShowFileTable => !FileListViewModel.IsLoadingFiles && FolderTreeViewModel.SelectedFolder is not null;

    public HomeViewModel()
    {
    }

    public HomeViewModel(
        IFileOperationStore fileOperationStore, 
        FolderTreeViewModel folderTreeViewModel,
        FileListViewModel fileListViewModel,
        SortOperationViewModel sortOperationViewModel,
        NotificationViewModel notificationViewModel,
        ISortReporter sortReporter
    )
    {
        _fileOperationStore = fileOperationStore;
        _sortReporter = sortReporter;

        FolderTreeViewModel = folderTreeViewModel;
        FileListViewModel = fileListViewModel;
        SortOperationViewModel = sortOperationViewModel;
        NotificationViewModel = notificationViewModel;

        SubscribeToViewModelEvents();

        _ = FolderTreeViewModel.InitializeAsync();
        _ = InitializeAsync();
    }

    private void SubscribeToViewModelEvents()
    {
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
