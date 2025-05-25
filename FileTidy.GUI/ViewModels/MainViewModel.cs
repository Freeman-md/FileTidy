using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFolderPath))]
    private FolderItem? _selectedFolder;

    [ObservableProperty]
    private bool isAllSelected;

    public string SelectedFolderPath => SelectedFolder != null ? BuildPath(SelectedFolder) : string.Empty;
    public string MockFolderSize => "2.7GB";
    public ObservableCollection<FolderItem> FolderTree { get; set; }
    
    [ObservableProperty] 
    private ObservableCollection<FileItem> _currentFiles;

    public MainViewModel()
    {
        FolderTree = BuildTreeWithParents();
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
    
    private ObservableCollection<FolderItem> BuildTreeWithParents()
    {
        var desktop = new FolderItem { Name = "Desktop" };
        var documents = new FolderItem { Name = "Documents", Parent = desktop };
        var downloads = new FolderItem { Name = "Downloads", Parent = desktop };

        var projects = new FolderItem { Name = "Projects", Parent = documents };
        var websites = new FolderItem { Name = "Websites", Parent = projects };
        var backups = new FolderItem { Name = "Backups", Parent = websites };

        websites.SubFolders.Add(new FolderItem { Name = "AllIcons", Parent = websites });
        websites.SubFolders.Add(backups);
        projects.SubFolders.Add(websites);
        projects.SubFolders.Add(new FolderItem { Name = "Backups", Parent = projects });
        documents.SubFolders.Add(projects);
        documents.SubFolders.Add(new FolderItem { Name = "Receipts", Parent = documents });

        desktop.SubFolders.Add(documents);
        desktop.SubFolders.Add(downloads);

        return new ObservableCollection<FolderItem> { desktop };
    }

    private string BuildPath(FolderItem folder)
    {
        var parts = new List<string>();
        var current = folder;

        while (current != null)
        {
            parts.Insert(0, current.Name);
            current = current.Parent;
        }
        
        return " / " + string.Join(" / ", parts);
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
}