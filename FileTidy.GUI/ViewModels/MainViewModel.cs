using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.Core.Services;
using FileTidy.Data.Sqlite;
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
        
    [ObservableProperty] private int _operationProgress = 0;
    [ObservableProperty] private int _filesProcessed = 0;
    [ObservableProperty] private string _elapsedTime = "0m 00s";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShouldShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShouldShowFileTable))]
    private bool _isLoadingFiles;

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
    [NotifyPropertyChangedFor(nameof(SelectedFolderPath))]
    [NotifyCanExecuteChangedFor(nameof(StartTidyingCommand))]
    [NotifyPropertyChangedFor(nameof(ShouldShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShouldShowFileTable))]
    private FolderItem? _selectedFolder;
    
    [ObservableProperty]
    private FolderItem? _selectedRootFolder;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RevertLastSortCommand))]
    private Guid _lastSortSessionId = Guid.Empty;

    [ObservableProperty]
    private bool _isAllSelected;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadableFolderSize))]
    private long _folderSize;
    
    [ObservableProperty] private string? _notificationTitle;
    [ObservableProperty] private string? _notificationMessage;
    [ObservableProperty] private bool _isNotificationVisible;


    [ObservableProperty]
    private ObservableCollection<FileItem> _currentFiles = new();

    public ObservableCollection<FolderItem> FolderTree { get; private set; } = new();
    
    public ObservableCollection<FolderItem> TopLevelFolders { get; private set; } = new();

    public int SelectedFileCount => CurrentFiles.Count(f => f.IsSelected);
    public int SelectedRevertableCount => CurrentFiles.Count(f => f.IsSelected && f.FileOperationStatus == FileOperationStatus.Moved);
    public bool CanRevertSelected => SelectedRevertableCount > 0;
    public bool CanDeleteSelected => SelectedFileCount > 0;
    private bool CanStartTidying => SelectedFolder is not null && IsSorting is false && IsReverting is false;
    private bool CanRevertLastSort() => LastSortSessionId != Guid.Empty && IsSorting == false && IsReverting == false;
    public string SelectedFolderPath => SelectedFolder?.FullPath ?? string.Empty;
    public bool ShouldShowEmptyState => !IsLoadingFiles && SelectedFolder is null;
    public bool ShouldShowFileTable => !IsLoadingFiles && SelectedFolder is not null;
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

    public string ReadableFolderSize => FolderSize.BytesToReadableSize();

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
        
        _ = InitializeAsync();
    }
    
    private void OnFileItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileItem.IsSelected))
        {
            IsAllSelected = CurrentFiles.All(item => item.IsSelected);
            OnPropertyChanged(nameof(SelectedFileCount));
            OnPropertyChanged(nameof(SelectedRevertableCount));
            OnPropertyChanged(nameof(CanRevertSelected));
            OnPropertyChanged(nameof(CanDeleteSelected));
            
            RevertSelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedFolderChanged(FolderItem? value)
    {
        _ = LoadFilesForSelectedFolder();

        if (value is null)
        {
            SelectedRootFolder = null;
            return;
        }
        
        var root = TopLevelFolders.FirstOrDefault(root => value.FullPath.StartsWith(root.FullPath, StringComparison.OrdinalIgnoreCase));
        
        if (root is not null)
            SelectedRootFolder = root;
    }

    partial void OnSelectedRootFolderChanged(FolderItem? value)
    {
        if (value is not null)
            SelectedFolder = value;
    }

    partial void OnIsAllSelectedChanged(bool oldValue, bool newValue)
    {
        bool userUncheckingManually = !newValue;
        bool notAllFilesSelected = CurrentFiles.Any(file => !file.IsSelected);

        if (userUncheckingManually && notAllFilesSelected)
            return;

        foreach (var file in CurrentFiles)
        {
            file.IsSelected = newValue;
        }
    }
    
    private async Task InitializeAsync()
    {
        await InitializeTopLevelFoldersAsync();
        await InitializeFolderTreeAsync();
        
        var sessionIdStr = await _fileOperationStore.GetConfigValueAsync(nameof(LastSortSessionId));
        if (Guid.TryParse(sessionIdStr, out var restoredId))
            LastSortSessionId = restoredId;

    }

    private async Task InitializeTopLevelFoldersAsync()
    {
        var rootFolders = await _folderService.GetTopLevelFoldersAsync().ConfigureAwait(false);
        await RunOnUIThreadAsync(() =>
        {
            TopLevelFolders = new ObservableCollection<FolderItem>(rootFolders);
            OnPropertyChanged(nameof(TopLevelFolders));
        });
    }

    private async Task InitializeFolderTreeAsync()
    {
        var folderTree = await _folderService.GetFolderTreeAsync().ConfigureAwait(false);
        
        foreach (var top in TopLevelFolders)
        {
            var match = folderTree.FirstOrDefault(f => f.FullPath == top.FullPath);
            if (match != null)
            {
                var index = folderTree.IndexOf(match);
                folderTree[index] = top;
                top.SubFolders = match.SubFolders;
            }
        }
        
        await RunOnUIThreadAsync(() =>
        {
            FolderTree = new ObservableCollection<FolderItem>(folderTree);
            OnPropertyChanged(nameof(FolderTree));
        });
    }

    private async Task LoadFilesForSelectedFolder()
    {
        if (SelectedFolder is null)
        {
            CurrentFiles = new();
            return;
        }

        IsLoadingFiles = true;

        try
        {
            
            var files = await _folderService.LoadFilesAsync(SelectedFolder.FullPath).ConfigureAwait(false);
            await SetFileItemsAsync(files);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading files: {ex.Message}");
            CurrentFiles = new();
        }
        finally
        {
            IsLoadingFiles = false;
        }
    }

    private async Task SetFileItemsAsync(List<FileItem> files)
    {
        if (SelectedFolder is null)
            return;
        
        var observableFiles = new ObservableCollection<FileItem>(files);
        
        var statuses = await _fileOperationLookupService.GetFileStatusesForDirectoryAsync(SelectedFolder.FullPath)
            .ConfigureAwait(false);

        foreach (var file in observableFiles)
        {
            file.PropertyChanged += OnFileItemPropertyChanged;

            if (!string.IsNullOrEmpty(file.FullPath) && statuses.TryGetValue(file.FullPath, out var status))
                file.FileOperationStatus = status;
        }

        FolderSize = files
            .Where(f => !f.IsFolder)
            .Sum(f =>
            {
                var path = Path.Combine(SelectedFolder!.FullPath, f.Name);
                return File.Exists(path) ? new FileInfo(path).Length : 0;
            });

        await RunOnUIThreadAsync(() => CurrentFiles = observableFiles);
    }

    private async Task RunOnUIThreadAsync(Action action)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action);
    }

    // TODO: Preserve folder tree state (expanded nodes, selected folder) after sorting
    // - Before refreshing FolderTree, store expanded paths and selected folder path
    // - Refresh FolderTree in-place or rebuild while restoring expansion/selection
    // - Ensure UI updates and bindings stay intact after refresh

    [RelayCommand(CanExecute = nameof(CanStartTidying))]
    private async Task StartTidying()
    {
        if (SelectedFolder is null)
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

            
            var result = await _fileTidyingService.SortDirectory(SelectedFolder.FullPath, LastSortSessionId, token);

            Console.WriteLine($"Tidying complete. Moved: {result.TotalMoved}, Errors: {result.TotalErrors}");

            _ = LoadFilesForSelectedFolder();
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

    [RelayCommand]
    private void OpenFolder(FileItem? fileItem)
    {
        if (fileItem is null || fileItem.IsFolder is false) return;

        SelectedFolder = new FolderItem
        {
            Name = fileItem.Name,
            FullPath = fileItem.FullPath!
        };
    }
    
    [RelayCommand] private async Task RevertSorting(FileItem fileItem)
    {
        if (fileItem?.FullPath is null)
            return;
        
        await _fileTidyingService.RevertFileAsync(fileItem.FullPath);

        CurrentFiles.Remove(fileItem);
        FolderSize -= fileItem.Size;
    }

    [RelayCommand] private async Task DeleteFile(FileItem fileItem)
    {
        if (fileItem.IsFolder || string.IsNullOrWhiteSpace(fileItem.FullPath))
            return;

        await _fileTidyingService.DeleteFileAsync(fileItem.FullPath);
        CurrentFiles.Remove(fileItem);

        FolderSize -= fileItem.Size;
    }
    
    [RelayCommand(CanExecute = nameof(CanRevertSelected))]
    private async Task RevertSelected()
    {
        var filesToRevert = CurrentFiles
            .Where(f => f.IsSelected && f.FileOperationStatus == FileOperationStatus.Moved)
            .ToList();

        foreach (var file in filesToRevert)
        {
            await _fileTidyingService.RevertFileAsync(file.FullPath!);
            CurrentFiles.Remove(file);
            FolderSize -= file.Size;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelected()
    {
        var filesToDelete = CurrentFiles
            .Where(f => f.IsSelected && !f.IsFolder)
            .ToList();

        foreach (var file in filesToDelete)
        {
            await _fileTidyingService.DeleteFileAsync(file.FullPath!);
            CurrentFiles.Remove(file);
            FolderSize -= file.Size;
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


        _ = LoadFilesForSelectedFolder();

        IsReverting = false;
    }
}
