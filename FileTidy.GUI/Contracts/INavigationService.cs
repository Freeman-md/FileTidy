using FileTidy.GUI.ViewModels;

namespace FileTidy.GUI.Contracts;

public interface INavigationService
{
    void NavigateTo(ViewModelBase viewModel);
}