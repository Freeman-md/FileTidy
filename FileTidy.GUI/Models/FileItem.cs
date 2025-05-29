using CommunityToolkit.Mvvm.ComponentModel;

namespace FileTidy.GUI.Models;

public partial class FileItem : ObservableObject
{
    [ObservableProperty] private bool isSelected;
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Size { get; set; }
    public required string Modified { get; set; }
    public required string Status { get; set; }
}