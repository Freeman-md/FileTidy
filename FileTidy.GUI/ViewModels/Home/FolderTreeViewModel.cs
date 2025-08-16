using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.GUI.Constants;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Helpers;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.ViewModels.Home;

public partial class FolderTreeViewModel : ViewModelBase
{
    private readonly IFolderService _folderService;
    private readonly Action<Action> _uiInvoker;


    public event Action<FolderItem?>? SelectedFolderChanged;

    public FolderTreeViewModel(IFolderService folderService, Action<Action>? uiInvoker = null)
    {
        _folderService = folderService;
        _uiInvoker = uiInvoker ?? (action => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action));
    } 
    
    [ObservableProperty]
    private FolderItem? _selectedFolder;
    
    [ObservableProperty]
    private FolderItem? _selectedRootFolder;
    
    public ObservableCollection<FolderItem> FolderTree { get; private set; } = new();
    
    public ObservableCollection<FolderItem> TopLevelFolders { get; private set; } = new();
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoAccessibleFolders))]
    private bool _hasAccessibleFolders;
    public bool HasNoAccessibleFolders => !HasAccessibleFolders;

    public async Task InitializeAsync()
    {
        await RefreshAccessAsync();
    }

    public async Task RefreshAccessAsync()
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
            HasAccessibleFolders = TopLevelFolders.Count > 0;

            // Clear selections when nothing is accessible
            if (!HasAccessibleFolders)
            {
                SelectedRootFolder = null;
                SelectedFolder = null;
            }

            OnPropertyChanged(nameof(HasNoAccessibleFolders));
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
    
    private Task RunOnUIThreadAsync(Action action)
    {
        _uiInvoker(action);
        return Task.CompletedTask;
    }
    
    [RelayCommand]
    private async Task OpenOsSettingsAsync()
    {
        await _folderService.OpenSystemFilesAndFoldersSettingsAsync();
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            await RefreshAccessAsync();
        });
        
        _ = Telemetry.LogAsync(TelemetryEventTypes.PermissionsOpenSettings);
    }

    [RelayCommand]
    private async Task RetryProbeAsync() => await RefreshAccessAsync();
}