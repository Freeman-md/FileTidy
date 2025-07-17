using System.Collections.ObjectModel;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.ViewModels.Onboarding;

public partial class FolderSelectionStepViewModel : ViewModelBase
{
    public ObservableCollection<SelectableFolder> Folders { get; } = new()
    {
        new SelectableFolder("Desktop"),
        new SelectableFolder("Downloads"),
        new SelectableFolder("Documents"),
    };
}