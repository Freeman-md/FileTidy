using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.ViewModels.Onboarding;
using FileTidy.GUI.Views.Onboarding;

namespace FileTidy.GUI.ViewModels.Layouts;

public partial class OnboardingWindowViewModel : ViewModelBase
{
    private readonly IFileOperationStore _fileOperationStore;
    public FolderSelectionStepViewModel FolderSelectionStepViewModel { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviousButton))]
    [NotifyPropertyChangedFor(nameof(ShowSkipButton))]
    [NotifyPropertyChangedFor(nameof(ShowPrimaryButton))]
    [NotifyPropertyChangedFor(nameof(CurrentStepView))]
    [NotifyPropertyChangedFor(nameof(PrimaryButtonText))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyCanExecuteChangedFor(nameof(Onboarding.OnboardingWindowViewModel.PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(Onboarding.OnboardingWindowViewModel.SkipStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(Onboarding.OnboardingWindowViewModel.NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(Onboarding.OnboardingWindowViewModel.PrimaryActionCommand))]
    private int _currentStepIndex;

    public bool ShowPreviousButton => CurrentStepIndex > 0 && CurrentStepIndex < _steps.Count - 1;
    public bool ShowSkipButton => CurrentStepIndex == 2;
    public bool ShowPrimaryButton => CurrentStepIndex != _steps.Count - 1;
    
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
        1 => "Select folders to organize:",
        2 => "Customize your experience",
        3 => "You're all set",
        _ => ""
    };
    
    public UserControl CurrentStepView => _steps[CurrentStepIndex];
    
    private readonly List<UserControl> _steps;
    
    
    public OnboardingWindowViewModel(IFileOperationStore fileOperationStore)
    {
        _fileOperationStore = fileOperationStore;

        _steps =
        [
            new WelcomeStepView(),
            new FolderSelectionStepView
            {
                DataContext = this
            },

            new PreferencesStepView()
            {
                DataContext = this
            },
            new CompletionStepView()
        ];
    }

    [RelayCommand]
    private async Task PrimaryAction()
    {
        switch (CurrentStepIndex)
        {
            case 0: // Welcome
            case 1: // Folder Selection
                NextStep();
                break;
            case 2: // Finish Setup
                // SaveUserPreferences();
                await SaveSelectedFoldersAsync();
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
    
    private async Task SaveSelectedFoldersAsync()
    {
        var selected = FolderSelectionStepViewModel.Folders
            .Where(f => f.IsSelected)
            .Select(f => f.Name)
            .ToList();

        if (selected.Any())
        {
            var serialized = string.Join(",", selected);
            await _fileOperationStore.SaveConfigValueAsync(AppConfigKeys.SelectedFolders, serialized);
        }

        await _fileOperationStore.SaveConfigValueAsync(AppConfigKeys.HasCompletedOnboarding, "true");
    }

}