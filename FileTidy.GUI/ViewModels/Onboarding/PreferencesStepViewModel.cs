using CommunityToolkit.Mvvm.ComponentModel;

namespace FileTidy.GUI.ViewModels.Onboarding;

public partial class PreferencesStepViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _askBeforeSorting;

    [ObservableProperty]
    private bool _darkModeEnabled;

    [ObservableProperty]
    private bool _sortByCategory = true;
}