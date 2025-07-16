using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.GUI.Views;
using FileTidy.GUI.Views.Onboarding;

namespace FileTidy.GUI.ViewModels.Onboarding;

public partial class OnboardingViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviousButton))]
    [NotifyPropertyChangedFor(nameof(ShowSkipButton))]
    [NotifyPropertyChangedFor(nameof(CurrentStepView))]
    [NotifyPropertyChangedFor(nameof(PrimaryButtonText))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(SkipStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    private int _currentStepIndex;

    public bool ShowPreviousButton => CurrentStepIndex > 0 && CurrentStepIndex < _steps.Count - 1;
    public bool ShowSkipButton => CurrentStepIndex == 2;
    
    public string PrimaryButtonText => CurrentStepIndex switch
    {
        0 => "Get Started",
        1 => "Continue",
        2 => "Finish Setup",
        _ => ""
    };

    public string StepTitle => CurrentStepIndex switch
    {
        0 => "Welcome to FileTidy",
        1 => "Choose which folders to organize",
        2 => "Customize your experience",
        3 => "You're all set",
        _ => ""
    };
    
    public UserControl CurrentStepView => _steps[CurrentStepIndex];
    
    private readonly List<UserControl> _steps =
    [
        new WelcomeStepView(),
        new FolderSelectionStepView(),
        new PreferencesStepView(),
        new CompletionStepView()
    ];

    [RelayCommand]
    private void PrimaryAction()
    {
        switch (CurrentStepIndex)
        {
            case 0: // Welcome
            case 1: // Folder Selection
                NextStep();
                break;
            case 2: // Finish Setup
                // SaveUserPreferences();
                NextStep(); // to Completion
                break;
        }
    }


    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStepIndex < _steps.Count - 1)
        {
            CurrentStepIndex++; 
            
            if (CurrentStepIndex == _steps.Count - 1)
                TriggerAutoLaunch();
        }
    }
    
    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
            CurrentStepIndex--;
    }
    
    [RelayCommand]
    private void SkipStep()
    {
        CurrentStepIndex = _steps.Count - 1;
        TriggerAutoLaunch();
    }

    private async void TriggerAutoLaunch()
    {
        await Task.Delay(2000);
        
        // trigger app view transition
    }
}