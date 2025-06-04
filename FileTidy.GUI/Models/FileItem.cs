using CommunityToolkit.Mvvm.ComponentModel;
using FileTidy.Core.Models;

namespace FileTidy.GUI.Models;

public partial class FileItem : ObservableObject
{
    [ObservableProperty] private bool isSelected;
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Size { get; set; }
    public required string Modified { get; set; }
    public FileOperationStatus? FileOperationStatus { get; set; } = Core.Models.FileOperationStatus.Unprocessed;
    public bool IsFolder => Type == "FOLDER";
    public string? FullPath { get; set; }
}