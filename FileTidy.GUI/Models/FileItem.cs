using CommunityToolkit.Mvvm.ComponentModel;
using FileTidy.Core.Models;
using FileTidy.GUI.Extensions;

namespace FileTidy.GUI.Models;

public partial class FileItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required long Size { get; set; }
    public required string Modified { get; set; }
    
    public string ReadableSize => Size > 0 ? Size.BytesToReadableSize() : "-";
    
    [ObservableProperty]
    private FileOperationStatus? _fileOperationStatus = Core.Models.FileOperationStatus.Unprocessed ;

    public bool IsFolder => Type == "FOLDER";
    public string? FullPath { get; set; }
}