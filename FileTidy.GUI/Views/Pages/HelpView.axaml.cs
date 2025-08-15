using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FileTidy.GUI.ViewModels.Pages;

namespace FileTidy.GUI.Views.Pages;

public partial class HelpView : UserControl
{
    public HelpView()
    {
        InitializeComponent();
    }
    
    // Left-nav anchor scroll
    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string targetName) return;
        var target = this.FindControl<Control>(targetName);
        target?.BringIntoView();
    }

    // Open image in lightbox
    private void OnImagePressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not HelpViewModel vm) return;
        if (sender is not Image img) return;
        var src = img.Tag as string;
        if (!string.IsNullOrWhiteSpace(src))
            vm.OpenLightbox(src);
    }

    // Click outside to close
    private void OnOverlayPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is HelpViewModel vm)
            vm.CloseLightbox();
    }
} 