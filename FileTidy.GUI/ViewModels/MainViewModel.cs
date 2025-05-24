using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private FolderItem? _selectedFolder;
    public ObservableCollection<FolderItem> FolderTree { get; set; } = new()
    {
        new FolderItem
        {
            Name = "Desktop",
            SubFolders = new List<FolderItem>()
            {
                new FolderItem
                {
                    Name = "Documents",
                    SubFolders = new List<FolderItem>
                    {
                        new FolderItem
                        {
                            Name = "Projects",
                            SubFolders = new List<FolderItem>() {
                               new FolderItem
                               {
                                   Name = "Websites",
                                   SubFolders = new List<FolderItem>() {
                                       new FolderItem { Name = "AllIcons" },
                                       new FolderItem { Name = "Backups" },
                                   }
                               },
                               new FolderItem { Name = "Backups" },
                            }
                        },
                        new FolderItem { Name = "Receipts" }
                    }
                },
                new FolderItem { Name = "Downloads" }
            }
                
        }
    };

    [ObservableProperty] private ObservableCollection<FileItem> _currentFiles = new()
    {
        new FileItem
        {
            Name = "Project_Proposal.pdf", Type = "PDF", Size = "1.2 MB", Modified = "May 12, 2025", Status = "Moved"
        },
        new FileItem
        {
            Name = "Screenshot_2025-05-01.png", Type = "PNG", Size = "345 KB", Modified = "May 1, 2025",
            Status = "Pending"
        },
        new FileItem
            { Name = "Budget_2025.xlsx", Type = "Excel", Size = "520 KB", Modified = "May 5, 2025", Status = "Moved" },
        new FileItem
        {
            Name = "Meeting_Notes.docx", Type = "Word", Size = "128 KB", Modified = "May 15, 2025",
            Status = "Unprocessed"
        }
    };
}