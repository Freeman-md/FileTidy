using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.Core.Services;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Extensions;
using FileTidy.GUI.Models;
using FileTidy.GUI.Reporting;

namespace FileTidy.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IFolderService _folderService;
    private readonly IFileOperationStore _fileOperationStore;
    private readonly IFileOperationLookupService _fileOperationLookupService;
    private readonly ISortReporter _sortReporter;
    private readonly IFileTidyingService _fileTidyingService;

    private CancellationTokenSource? _sortingCancellationTokenSource;
    
    public FolderTreeViewModel FolderTreeViewModel { get; }
    public FileListViewModel FileListViewModel { get; }
        
    [ObservableProperty] private int _operationProgress = 0;
    [ObservableProperty] private int _filesProcessed = 0;
    [ObservableProperty] private string _elapsedTime = "0m 00s";
    
    public string AppVersion => $"FileTidy v{Assembly.GetExecutingAssembly().GetName().Version} | Built by Freemancodz";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopSortingCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertLastSortCommand))]
    [NotifyPropertyChangedFor(nameof(CurrentOperationLabel))]
    private bool _isSorting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTidyingCommand))]
    [NotifyPropertyChangedFor(nameof(CurrentOperationLabel))]
    private bool _isReverting;
    
    [ObservableProperty] private bool _wasCancelled;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RevertLastSortCommand))]
    private Guid _lastSortSessionId = Guid.Empty;
    
    [ObservableProperty] private string? _notificationTitle;
    [ObservableProperty] private string? _notificationMessage;
    [ObservableProperty] private bool _isNotificationVisible;
    
    private bool CanStartTidying => FolderTreeViewModel.SelectedFolder is not null && IsSorting is false && IsReverting is false;
    private bool CanRevertLastSort() => LastSortSessionId != Guid.Empty && IsSorting == false && IsReverting == false;
    public string SelectedFolderPath => FolderTreeViewModel.SelectedFolder?.FullPath ?? string.Empty;
    public bool ShouldShowEmptyState => !FileListViewModel.IsLoadingFiles && FolderTreeViewModel.SelectedFolder is null;
    public bool ShouldShowFileTable => !FileListViewModel.IsLoadingFiles && FolderTreeViewModel.SelectedFolder is not null;
    public string CurrentOperationLabel
    {
        get
        {
            if (IsReverting)
                return "files reverted";
            if (IsSorting)
                return "files sorted";
            return "files processed";
        }
    }

    public MainViewModel() { }

    public MainViewModel(
        IFolderService folderService, 
        IFileOperationStore fileOperationStore, 
        IFileOperationLookupService fileOperationLookupService
    )
    {
        _folderService = folderService;
        _fileOperationStore = fileOperationStore;
        _fileOperationLookupService = fileOperationLookupService;
        _sortReporter = new GuiFileSortReporter(
            progress => OperationProgress = progress,
            elapsed => ElapsedTime = elapsed,
            filesProcessed => FilesProcessed = filesProcessed,
            (title, message) =>
            {
                NotificationTitle = title;
                NotificationMessage = message;
                IsNotificationVisible = true;
                
                Task.Delay(4000).ContinueWith(_ =>
                {
                    IsNotificationVisible = false;
                });
            }
        );
        _fileTidyingService = new FileTidyingService(_fileOperationStore, _sortReporter);

        FolderTreeViewModel = new FolderTreeViewModel(_folderService);
        FileListViewModel = new FileListViewModel(FolderTreeViewModel, _folderService, _fileOperationLookupService, _fileTidyingService);

        FolderTreeViewModel.PropertyChanged += (sender, propertyChangedArgs) =>
        {
            if (propertyChangedArgs.PropertyName == nameof(FolderTreeViewModel.SelectedFolder))
            {
                OnPropertyChanged(nameof(SelectedFolderPath));
                OnPropertyChanged(nameof(ShouldShowEmptyState));
                OnPropertyChanged(nameof(ShouldShowFileTable));
                StartTidyingCommand.NotifyCanExecuteChanged();
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
        var sessionIdStr = await _fileOperationStore.GetConfigValueAsync(nameof(LastSortSessionId));
        if (Guid.TryParse(sessionIdStr, out var restoredId))
            LastSortSessionId = restoredId;
    }

    // TODO: Preserve folder tree state (expanded nodes, selected folder) after sorting
    // - Before refreshing FolderTree, store expanded paths and selected folder path
    // - Refresh FolderTree in-place or rebuild while restoring expansion/selection
    // - Ensure UI updates and bindings stay intact after refresh

    [RelayCommand(CanExecute = nameof(CanStartTidying))]
    private async Task StartTidying()
    {
        if (FolderTreeViewModel.SelectedFolder is null)
            return;

        try
        {
            IsSorting = true;
            OperationProgress = 0;
            FilesProcessed = 0;
            ElapsedTime = "0m 00s";
            WasCancelled = false;
            
            _sortingCancellationTokenSource = new CancellationTokenSource();
            var token = _sortingCancellationTokenSource.Token;
            
            LastSortSessionId = Guid.NewGuid();
            await _fileOperationStore.SaveConfigValueAsync(nameof(LastSortSessionId), LastSortSessionId.ToString());

            
            var result = await _fileTidyingService.SortDirectory(FolderTreeViewModel.SelectedFolder.FullPath, LastSortSessionId, token);

            Console.WriteLine($"Tidying complete. Moved: {result.TotalMoved}, Errors: {result.TotalErrors}");

            _ = FileListViewModel.LoadFilesForSelectedFolder();
            // Instead of doing this, let's just update the current folder with the new structure directly
            // _ = InitializeFolderTreeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tidying failed: {ex.Message}");
        }
        finally
        {
            IsSorting = false;
            
            _sortingCancellationTokenSource?.Dispose();
            _sortingCancellationTokenSource = null;
        }
    }

    [RelayCommand] private void PauseSorting() => Console.WriteLine("Pause sorting triggered");
    
    [RelayCommand(CanExecute = nameof(IsSorting))] 
    private void StopSorting() {
        if (_sortingCancellationTokenSource is not null)
        {
            _sortingCancellationTokenSource?.Cancel();
            _sortingCancellationTokenSource = null;
            IsSorting = false;
            WasCancelled = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRevertLastSort))]
    private async Task RevertLastSort()
    {
        IsReverting = true;
        OperationProgress = 0;
        FilesProcessed = 0;
        ElapsedTime = "0m 00s";
        
        if (LastSortSessionId == Guid.Empty)
            return;

        await _fileTidyingService.RevertSessionAsync(LastSortSessionId);
        
        LastSortSessionId = Guid.Empty;
        await _fileOperationStore.DeleteConfigValueAsync(nameof(LastSortSessionId));


        _ = FileListViewModel.LoadFilesForSelectedFolder();

        IsReverting = false;
    }
}
