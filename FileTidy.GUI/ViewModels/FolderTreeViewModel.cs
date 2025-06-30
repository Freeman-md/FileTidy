using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FileTidy.GUI.Models;
using System.Threading.Tasks;
using FileTidy.GUI.Contracts;

namespace FileTidy.GUI.ViewModels;

public partial class FolderTreeViewModel : ViewModelBase
{
    private readonly IFolderService _folderService;

    public event Action<FolderItem?>? SelectedFolderChanged;

    public FolderTreeViewModel(IFolderService folderService)
    {
        _folderService = folderService;
    } 
    
    [ObservableProperty]
    private FolderItem? _selectedFolder;
    
    [ObservableProperty]
    private FolderItem? _selectedRootFolder;
    
    public ObservableCollection<FolderItem> FolderTree { get; private set; } = new();
    
    public ObservableCollection<FolderItem> TopLevelFolders { get; private set; } = new();

    public async Task InitializeAsync()
    {
        await InitializeFolderTreeAsync();
        await InitializeTopLevelFoldersAsync();
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
    
    partial void OnSelectedFolderChanged(FolderItem? value)
    {
        SelectedFolderChanged?.Invoke(value);

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
    
    private async Task RunOnUIThreadAsync(Action action)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action);
    }
}