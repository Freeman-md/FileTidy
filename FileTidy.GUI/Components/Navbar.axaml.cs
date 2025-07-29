using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.ViewModels;
using FileTidy.GUI.Services;
using FileTidy.GUI.ViewModels.Home;
using FileTidy.GUI.ViewModels.Layouts;

namespace FileTidy.GUI.Components;

public partial class Navbar : UserControl
{

    public Navbar()
    {
        InitializeComponent();
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.GoToSettingsCommand.Execute(null);
    }

    private void HelpButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.GoToHelpCommand.Execute(null);
    }

    private void LogoImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.GoToHomeCommand.Execute(null);
    }
}