using FileTidy.GUI.Contracts;
using FileTidy.GUI.ViewModels;

namespace FileTidy.GUI.Services
{
    public class NavigationService : INavigationService
    {
        private readonly RootViewModel _rootViewModel;

        public NavigationService(RootViewModel rootViewModel)
        {
            _rootViewModel = rootViewModel;
        }

        public void NavigateTo(ViewModelBase viewModel)
        {
            _rootViewModel.CurrentViewModel = viewModel;
        }
    }
} 