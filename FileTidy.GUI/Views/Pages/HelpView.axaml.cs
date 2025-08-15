using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FileTidy.GUI.Views.Pages;

public partial class HelpView : UserControl
{
    public HelpView()
    {
        InitializeComponent();
    }
    
    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string targetName)
            return;

        var target = this.FindControl<Control>(targetName);
        target?.BringIntoView();
    }
} 