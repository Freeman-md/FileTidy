using Avalonia.Controls;
using Avalonia.Input;
using FileTidy.GUI.Models;
using FileTidy.GUI.ViewModels;

namespace FileTidy.GUI.Components;

public partial class FolderPanel : UserControl
{
    public FolderPanel()
    {
        InitializeComponent();
    }

    private void FileItemNameTextBlock_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock { DataContext: FileItem { IsFolder: true } fileItem })
        {
            if (DataContext is MainViewModel vm)
            {
                vm.FileListViewModel.OpenFolderCommand.Execute(fileItem);
            }
        }
    }
}