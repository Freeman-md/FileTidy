using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.ViewModels;
using FileTidy.GUI.Services;

namespace FileTidy.GUI.Components;

public partial class Navbar : UserControl
{
    private readonly INavigationService _navigationService;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly HelpViewModel _helpViewModel;
    private readonly HomeViewModel _homeViewModel;

    public Navbar()
    {
        InitializeComponent();
        _navigationService = (INavigationService)App.Services.GetService(typeof(INavigationService));
        _settingsViewModel = (SettingsViewModel)App.Services.GetService(typeof(SettingsViewModel));
        _helpViewModel = (HelpViewModel)App.Services.GetService(typeof(HelpViewModel));
        _homeViewModel = (HomeViewModel)App.Services.GetService(typeof(HomeViewModel));
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        _navigationService?.NavigateTo(_settingsViewModel);
    }

    private void HelpButton_Click(object? sender, RoutedEventArgs e)
    {
        _navigationService?.NavigateTo(_helpViewModel);
    }

    private void LogoImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _navigationService?.NavigateTo(_homeViewModel);
    }
}