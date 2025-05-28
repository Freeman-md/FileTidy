using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Extensions;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IFolderService _folderService;

#if DEBUG
    public string MockFolderSize => "2.7GB";
    [ObservableProperty] private int _sortProgress = 75;
    [ObservableProperty] private int _sortedFiles = 527;
    [ObservableProperty] private string _elapsedTime = "2m 34s";
#endif

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShouldShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShouldShowFileTable))]
    private bool _isLoadingFiles;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFolderPath))]
    [NotifyCanExecuteChangedFor(nameof(StartTidyingCommand))]
    [NotifyPropertyChangedFor(nameof(ShouldShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShouldShowFileTable))]
    private FolderItem? _selectedFolder;

    [ObservableProperty]
    private bool _isAllSelected;

    [ObservableProperty]
    private ObservableCollection<FileItem> _currentFiles = new();

    public ObservableCollection<FolderItem> FolderTree { get; private set; } = new();

    private bool CanStartTidying => SelectedFolder is not null;

    public string SelectedFolderPath => SelectedFolder?.FullPath ?? string.Empty;
    public bool ShouldShowEmptyState => !IsLoadingFiles && SelectedFolder is null;
    public bool ShouldShowFileTable => !IsLoadingFiles && SelectedFolder is not null;

    public MainViewModel() { }

    public MainViewModel(IFolderService folderService)
    {
        _folderService = folderService;
        _ = InitializeFolderTreeAsync();
    }

    private async Task InitializeFolderTreeAsync()
    {
        var rootFolders = await _folderService.GetSystemRootFolders().ConfigureAwait(false);
        await RunOnUIThreadAsync(() =>
        {
            FolderTree = new ObservableCollection<FolderItem>(rootFolders);
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
        var observableFiles = new ObservableCollection<FileItem>(files);
        foreach (var file in observableFiles)
            file.PropertyChanged += OnFileItemPropertyChanged;

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

    [RelayCommand(CanExecute = nameof(CanStartTidying))]
    private void StartTidying() => Console.WriteLine("Start tidying triggered");

    [RelayCommand] private void PauseSorting() => Console.WriteLine("Pause sorting triggered");
    [RelayCommand] private void StopSorting() => Console.WriteLine("Stop sorting triggered");
    [RelayCommand] private void RevertSorting() => Console.WriteLine("Revert clicked");
}
