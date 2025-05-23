using CommunityToolkit.Mvvm.ComponentModel;

namespace FileTidy.GUI.Models;

public partial class FileItem : ObservableObject
{
    [ObservableProperty] private bool isSelected;
    public string Name { get; set; }
    public string Type { get; set; }
    public string Size { get; set; }
    public string Modified { get; set; }
    public string Status { get; set; }
}