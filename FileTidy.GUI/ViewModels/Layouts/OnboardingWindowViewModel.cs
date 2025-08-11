using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;
using FileTidy.GUI.Views.Onboarding.Steps;

namespace FileTidy.GUI.ViewModels.Layouts;

public partial class OnboardingWindowViewModel : ViewModelBase
{
    private readonly IFileOperationStore _fileOperationStore;
    private readonly IFolderService _folderService;
    private readonly IAppConfigService _appConfig;

    // Current step
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviousButton))]
    [NotifyPropertyChangedFor(nameof(ShowPrimaryButton))]
    [NotifyPropertyChangedFor(nameof(CurrentStepView))]
    [NotifyPropertyChangedFor(nameof(PrimaryButtonText))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(StepPreviewSource))]
    private int _currentStepIndex;

    // Access statuses
    [ObservableProperty] private bool _desktopGranted;
    [ObservableProperty] private bool _downloadsGranted;
    [ObservableProperty] private bool _documentsGranted;

    public bool AtLeastOneGranted => DesktopGranted || DownloadsGranted || DocumentsGranted;

    public bool ShowPreviousButton => CurrentStepIndex > 0 && CurrentStepIndex < _steps.Count - 1;
    public bool ShowPrimaryButton => true;

    private readonly string[] _stepPreviewImages =
    {
        "avares://FileTidy.GUI/Assets/Images/onboarding/welcome.png",
        "avares://FileTidy.GUI/Assets/Images/onboarding/access.png",
        "avares://FileTidy.GUI/Assets/Images/onboarding/background.png",
    };

    public string StepPreviewSource =>
        _stepPreviewImages[Math.Clamp(CurrentStepIndex, 0, _stepPreviewImages.Length - 1)];

    public string PrimaryButtonText => CurrentStepIndex switch
    {
        0 => "Get Started",
        1 => AtLeastOneGranted ? "Continue" : "Grant Access",
        2 => "Start FileTidy",
        _ => ""
    };

    public string StepTitle => CurrentStepIndex switch
    {
        0 => "Welcome to FileTidy",
        1 => "Grant Folder Access",
        2 => "You're all set",
        _ => ""
    };

    public UserControl CurrentStepView => _steps[CurrentStepIndex];
    private readonly List<UserControl> _steps;

    public OnboardingWindowViewModel(
        IFileOperationStore fileOperationStore,
        IFolderService folderService,
        IAppConfigService appConfig)
    {
        _fileOperationStore = fileOperationStore;
        _folderService = folderService;
        _appConfig = appConfig;

        _steps =
        [
            new WelcomeStepView(),
            new AccessStepView { DataContext = this },
            new CompletionStepView()
        ];

        _ = ProbeAllAsync();
    }

    [RelayCommand]
    private async Task ProbeAllAsync()
    {
        DesktopGranted   = await _folderService.CanAccessAsync(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        DownloadsGranted = await _folderService.CanAccessAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        DocumentsGranted = await _folderService.CanAccessAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        OnPropertyChanged(nameof(AtLeastOneGranted));
        OnPropertyChanged(nameof(PrimaryButtonText));
    }

    [RelayCommand]
    private async Task PrimaryActionAsync()
    {
        switch (CurrentStepIndex)
        {
            case 0:
                NextStep();
                break;

            case 1:
                // If none granted, try probe again to trigger OS prompt
                if (!AtLeastOneGranted)
                {
                    await ProbeAllAsync();
                    return;
                }

                // Persist granted folders (comma list or keys)
                var granted = new List<string>();
                if (DesktopGranted) granted.Add("Desktop");
                if (DownloadsGranted) granted.Add("Downloads");
                if (DocumentsGranted) granted.Add("Documents");

                if (granted.Count > 0)
                    await _fileOperationStore.SaveConfigValueAsync(AppConfigKeys.SelectedFolders, string.Join(",", granted));

                await _appConfig.SetHasCompletedOnboardingAsync(true);
                NextStep();
                break;

            case 2:
                // trigger main app transition
                await Task.Delay(300);
                // close onboarding / open MainWindow (handled outside or via event)
                break;
        }
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStepIndex < _steps.Count - 1)
            CurrentStepIndex++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
            CurrentStepIndex--;
    }
}
