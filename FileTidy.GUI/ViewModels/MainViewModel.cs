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

    private CancellationTokenSource? _sortingCancellationTokenSource;
        
    [ObservableProperty] private int _sortProgress = 0;
    [ObservableProperty] private int _sortedFiles = 0;
    [ObservableProperty] private string _elapsedTime = "0m 00s";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShouldShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShouldShowFileTable))]
    private bool _isLoadingFiles;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopSortingCommand))]
    private bool _isSorting;
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
    private bool _isAllSelected;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadableFolderSize))]
    private long _folderSize;

    [ObservableProperty]
    private ObservableCollection<FileItem> _currentFiles = new();

    public ObservableCollection<FolderItem> FolderTree { get; private set; } = new();
    
    public ObservableCollection<FolderItem> TopLevelFolders { get; private set; } = new();

    private bool CanStartTidying => SelectedFolder is not null && IsSorting is false;
    public string SelectedFolderPath => SelectedFolder?.FullPath ?? string.Empty;
    public bool ShouldShowEmptyState => !IsLoadingFiles && SelectedFolder is null;
    public bool ShouldShowFileTable => !IsLoadingFiles && SelectedFolder is not null;
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
        
        _ = InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        await InitializeTopLevelFoldersAsync();
        await InitializeFolderTreeAsync();
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
        
        Console.WriteLine("==== Status Dictionary ====");
        foreach (var entry in statuses)
        {
            Console.WriteLine($"[STATUS MAP] {entry.Key} → {entry.Value}");
        }
        Console.WriteLine("============================");


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
        
        // Console.WriteLine("==== File Statuses ====");
        // foreach (var file in observableFiles)
        // {
        //     Console.WriteLine($"• {file.Name}");
        //     Console.WriteLine($"  ↳ Full Path: {file.FullPath}");
        //     Console.WriteLine($"  ↳ Status: {file.FileOperationStatus}");
        // }
        // Console.WriteLine("========================");

        await RunOnUIThreadAsync(() => CurrentFiles = observableFiles);
    }

    private async Task RunOnUIThreadAsync(Action action)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action);
    }

    private void OnFileItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileItem.IsSelected))
        {
            IsAllSelected = CurrentFiles.All(item => item.IsSelected);
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
            SortProgress = 0;
            SortedFiles = 0;
            ElapsedTime = "0m 00s";
            WasCancelled = false;
            
            _sortingCancellationTokenSource = new CancellationTokenSource();
            var token = _sortingCancellationTokenSource.Token;

            var reporter = new GuiFileSortReporter(
            progress => SortProgress = progress,
            elapsed => ElapsedTime = elapsed,
            filesMoved => SortedFiles = filesMoved
            );
            
            var tidyService = new FileTidyingService(_fileOperationStore, reporter);
            var result = await tidyService.SortDirectory(SelectedFolder.FullPath, token);

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
    
    [RelayCommand] private void RevertSorting() => Console.WriteLine("Revert clicked");
}
