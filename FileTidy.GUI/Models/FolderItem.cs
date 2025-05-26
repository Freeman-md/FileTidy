using System.Collections.Generic;

namespace FileTidy.GUI.Models;

public class FolderItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public List<FolderItem> SubFolders { get; set; } = new();
    public bool IsExpanded { get; set; }
    public FolderItem? Parent { get; set; }
}