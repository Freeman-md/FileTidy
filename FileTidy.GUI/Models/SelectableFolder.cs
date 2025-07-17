using CommunityToolkit.Mvvm.ComponentModel;

namespace FileTidy.GUI.Models;

public partial class SelectableFolder(string name) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    private bool _isSelected;
}