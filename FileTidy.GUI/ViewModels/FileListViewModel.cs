using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Extensions;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.ViewModels;

public partial class FileListViewModel : ViewModelBase
{
    private readonly FolderTreeViewModel _folderTreeViewModel;
    private readonly IFolderService _folderService;
    private readonly IFileStatusService _fileStatusService;
    private readonly IFileOrganizerService _fileOrganizerService;

    public FileListViewModel(
        FolderTreeViewModel folderTreeViewModel,
        IFolderService folderService, 
        IFileStatusService fileStatusService,
        IFileOrganizerService fileOrganizerService)
    {
        _folderTreeViewModel = folderTreeViewModel;
        _folderService = folderService;
        _fileStatusService = fileStatusService;
        _fileOrganizerService = fileOrganizerService;
    }
    
    
    [ObservableProperty]
    private bool _isAllSelected;
    
    [ObservableProperty]
    private bool _isLoadingFiles;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadableFolderSize))]
    private long _folderSize;
    
    [ObservableProperty]
    private ObservableCollection<FileItem> _currentFiles = new();
    
    public int SelectedFileCount => CurrentFiles.Count(f => f.IsSelected);
    public int SelectedRevertableCount => CurrentFiles.Count(f => f.IsSelected && f.FileOperationStatus == FileOperationStatus.Moved);
    public bool CanRevertSelected => SelectedRevertableCount > 0;
    public bool CanDeleteSelected => SelectedFileCount > 0;
    
    public string ReadableFolderSize => FolderSize.BytesToReadableSize();
    
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
    
    public async Task LoadFilesForSelectedFolder()
    {
        if (_folderTreeViewModel.SelectedFolder is null)
        {
            CurrentFiles = new();
            return;
        }

        IsLoadingFiles = true;

        try
        {
            
            var files = await _folderService.LoadFilesAsync(_folderTreeViewModel.SelectedFolder.FullPath).ConfigureAwait(false);
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
        if (_folderTreeViewModel.SelectedFolder is null)
            return;
        
        var observableFiles = new ObservableCollection<FileItem>(files);
        
        var statuses = await _fileStatusService.GetFileStatusesForDirectoryAsync(_folderTreeViewModel.SelectedFolder.FullPath)
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
                var path = Path.Combine(_folderTreeViewModel.SelectedFolder!.FullPath, f.Name);
                return File.Exists(path) ? new FileInfo(path).Length : 0;
            });

        await RunOnUIThreadAsync(() => CurrentFiles = observableFiles);
    }
    
    private async Task RunOnUIThreadAsync(Action action)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action);
    }
    
    [RelayCommand]
    private void OpenFolder(FileItem? fileItem)
    {
        if (fileItem is null || fileItem.IsFolder is false) return;

        _folderTreeViewModel.SelectedFolder = new FolderItem
        {
            Name = fileItem.Name,
            FullPath = fileItem.FullPath!
        };
    }
    
    [RelayCommand] private async Task RevertSorting(FileItem fileItem)
    {
        if (fileItem?.FullPath is null)
            return;
        
        await _fileOrganizerService.RevertFileAsync(fileItem.FullPath);

        CurrentFiles.Remove(fileItem);
        FolderSize -= fileItem.Size;
    }
    
    [RelayCommand] private async Task DeleteFile(FileItem fileItem)
    {
        if (fileItem.IsFolder || string.IsNullOrWhiteSpace(fileItem.FullPath))
            return;

        await _fileOrganizerService.DeleteFileAsync(fileItem.FullPath);
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
            await _fileOrganizerService.RevertFileAsync(file.FullPath!);
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
            await _fileOrganizerService.DeleteFileAsync(file.FullPath!);
            CurrentFiles.Remove(file);
            FolderSize -= file.Size;
        }
    }
}