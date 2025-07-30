using Avalonia.Controls;
using Avalonia.Input;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.Views.Onboarding.Steps;

public partial class FolderSelectionStepView : UserControl
{
    public FolderSelectionStepView()
    {
        InitializeComponent();
    }

    private void FolderItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is SelectableFolder folder)
        {
            folder.IsSelected = !folder.IsSelected;
        }
    }
}