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
    public string MockFolderSize => "2.7GB";
    [ObservableProperty]
    private int _sortProgress = 75;
    [ObservableProperty]
    private int _sortedFiles = 527;
    [ObservableProperty]
    private string _elapsedTime = "2m 34s";

    [NotifyPropertyChangedFor(nameof(ShouldShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShouldShowFileTable))]
    [ObservableProperty] private bool _isLoadingFiles;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFolderPath))]
    [NotifyCanExecuteChangedFor(nameof(StartTidyingCommand))]
    [NotifyPropertyChangedFor(nameof(ShouldShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShouldShowFileTable))]
    private FolderItem? _selectedFolder;

    [ObservableProperty]
    private bool _isAllSelected;
    
    [ObservableProperty] private ObservableCollection<FileItem> _currentFiles = new();
    public ObservableCollection<FolderItem> FolderTree { get; set; }
    
    private bool CanStartTidying => SelectedFolder != null;

    public string SelectedFolderPath => SelectedFolder != null ? BuildPath(SelectedFolder) : string.Empty;
    public bool ShouldShowEmptyState => !IsLoadingFiles && SelectedFolder == null;
    public bool ShouldShowFileTable => !IsLoadingFiles && SelectedFolder != null;


    public MainViewModel()
    {
        
    }

    public MainViewModel(IFolderService folderService)
    {
        _folderService = folderService;
        
        FolderTree = _folderService.GetSystemRootFolders();
    }

    private async Task LoadFilesForSelectedFolder()
    {
        if (SelectedFolder is null || string.IsNullOrWhiteSpace(SelectedFolder.FullPath))
        {
            CurrentFiles = new();
            return;
        }

        IsLoadingFiles = true;

        try
        {
            var filePaths = Directory.GetFiles(SelectedFolder.FullPath);
            var files = filePaths.Select(path => new FileItem
            {
                Name = Path.GetFileName(path),
                Type = Path.GetExtension(path).TrimStart('.').ToUpper(),
                Size = new FileInfo(path).Length.BytesToReadableSize(),
                Modified = File.GetLastWriteTime(path).ToString("MMM dd, yyyy"),
                Status = "Unprocessed"
            });
        
            var fileItems = new ObservableCollection<FileItem>(files);
            foreach (var fileItem in fileItems)
                fileItem.PropertyChanged += OnFileItemPropertyChanged;
            
            CurrentFiles = fileItems;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            CurrentFiles = new();
        }
        finally
        {
            IsLoadingFiles = false;
        }
    }
    
    private string BuildPath(FolderItem folder)
    {
        return folder.FullPath;
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
    private void StartTidying()
    {
        Console.WriteLine("Start tidying triggered");
    }
    
    [RelayCommand]
    private void PauseSorting() => Console.WriteLine("Pause sorting triggered");
    [RelayCommand]
    private void StopSorting() => Console.WriteLine("Stop sorting triggered");
    [RelayCommand]
    private void RevertSorting() => Console.WriteLine("Revert clicked");
}