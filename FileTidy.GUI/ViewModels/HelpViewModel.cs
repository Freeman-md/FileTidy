using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.GUI.Contracts;

namespace FileTidy.GUI.ViewModels
{
    public partial class HelpViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly HomeViewModel _homeViewModel;

        public HelpViewModel(INavigationService navigationService, HomeViewModel homeViewModel)
        {
            _navigationService = navigationService;
            _homeViewModel = homeViewModel;
        }

        [RelayCommand]
        private void GoHome()
        {
            _navigationService.NavigateTo(_homeViewModel);
        }
    }
} 