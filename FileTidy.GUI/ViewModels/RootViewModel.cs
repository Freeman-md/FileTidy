namespace FileTidy.GUI.ViewModels
{
    public class RootViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public RootViewModel(ViewModelBase initialViewModel)
        {
            _currentViewModel = initialViewModel;
        }
    }
}