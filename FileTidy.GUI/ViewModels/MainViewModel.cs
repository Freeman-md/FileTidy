using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IFolderService _folderService;
    public string MockFolderSize => "2.7GB";
    [ObservableProperty]
    private int _sortProgress = 75;
    [ObservableProperty]
    private int sortedFiles = 527;
    [ObservableProperty]
    private string _elapsedTime = "2m 34s";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFolderPath))]
    [NotifyCanExecuteChangedFor(nameof(StartTidyingCommand))]
    private FolderItem? _selectedFolder;

    [ObservableProperty]
    private bool isAllSelected;
    public ObservableCollection<FolderItem> FolderTree { get; set; }
    
    private bool CanStartTidying => SelectedFolder != null;

    public string SelectedFolderPath => SelectedFolder != null ? BuildPath(SelectedFolder) : string.Empty;
    
    [ObservableProperty] 
    private ObservableCollection<FileItem> _currentFiles;

    public MainViewModel()
    {
        
    }

    public MainViewModel(IFolderService folderService)
    {
        _folderService = folderService;
        
        FolderTree = _folderService.GetSystemRootFolders();
        CurrentFiles = LoadFiles();
    }

    private ObservableCollection<FileItem> LoadFiles()
    {
        var files = new ObservableCollection<FileItem>
        {
            new FileItem
            {
                Name = "Project_Proposal.pdf", Type = "PDF", Size = "1.2 MB", Modified = "May 12, 2025",
                Status = "Moved"
            },
            new FileItem
            {
                Name = "Screenshot_2025-05-01.png", Type = "PNG", Size = "345 KB", Modified = "May 1, 2025",
                Status = "Pending"
            },
            new FileItem
            {
                Name = "Budget_2025.xlsx", Type = "Excel", Size = "520 KB", Modified = "May 5, 2025", Status = "Moved"
            },
            new FileItem
            {
                Name = "Meeting_Notes.docx", Type = "Word", Size = "128 KB", Modified = "May 15, 2025",
                Status = "Unprocessed"
            }
        };

        foreach (var file in files)
            file.PropertyChanged += OnFileItemPropertyChanged;

        return files;
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