using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.GUI.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace FileTidy.GUI.ViewModels.Layouts;

public enum PageType
{
    Home,
    Settings,
    Help
}

    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IServiceProvider _services;
        
        [ObservableProperty]
        private ViewModelBase? _currentViewModel;
        
        [ObservableProperty]
        private PageType _currentPage;
        
        public string AppVersion => $"FileTidy v{Assembly.GetExecutingAssembly().GetName().Version} | Built by Freemancodz";

        public MainWindowViewModel(IServiceProvider services)
        {
            _services = services;
            GoToHome();
        }

        [RelayCommand]
        private void GoToHome()
        {
            CurrentViewModel = _services.GetRequiredService<HomeViewModel>();
            CurrentPage = PageType.Home;
        }
        
        [RelayCommand]
        private void GoToSettings()
        {
            CurrentViewModel = _services.GetRequiredService<SettingsViewModel>();
            CurrentPage = PageType.Settings;
        }
        
        [RelayCommand]
        private void GoToHelp()
        {
            CurrentViewModel = _services.GetRequiredService<HelpViewModel>();
            CurrentPage = PageType.Help;
        }
    }
