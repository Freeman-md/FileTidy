using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTidy.Core.Interfaces;
using FileTidy.GUI.Constants;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Helpers;
using FileTidy.GUI.Services;
using FileTidy.GUI.Views;
using FileTidy.GUI.Views.Onboarding.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileTidy.GUI.ViewModels.Layouts;

public partial class OnboardingWindowViewModel : ViewModelBase
{
    // =========================
    // Dependencies
    // =========================
    private readonly IFileOperationStore _fileOperationStore;
    private readonly IFolderService _folderService;
    private readonly IAppConfigService _appConfig;
    private readonly IDeviceTelemetryService _deviceTelemetryService;

    // =========================
    // Paths
    // =========================
    public string DesktopPath => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public string DocumentsPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public string DownloadsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    // =========================
    // Step State
    // =========================
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviousButton))]
    [NotifyPropertyChangedFor(nameof(CurrentStepView))]
    [NotifyPropertyChangedFor(nameof(PrimaryButtonText))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(StepPreviewSource))]
    [NotifyPropertyChangedFor(nameof(StepPreviewBinding))]
    [NotifyPropertyChangedFor(nameof(PreviewImageHorizontalAlignment))]
    [NotifyPropertyChangedFor(nameof(PreviewContainerMargin))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private int _currentStepIndex;

    private readonly List<UserControl> _steps;
    public int StepsCount => _steps.Count;
    public int StepsCountMinusOne => Math.Max(0, StepsCount - 1);

    // =========================
    // Access State
    // =========================
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AtLeastOneGranted))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _desktopGranted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AtLeastOneGranted))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _downloadsGranted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AtLeastOneGranted))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _documentsGranted;
    
    public bool AtLeastOneGranted => DesktopGranted || DownloadsGranted || DocumentsGranted;

    // =========================
    // View Bindings
    // =========================
    public bool ShowPreviousButton => CurrentStepIndex > 0 && CurrentStepIndex < _steps.Count - 1;
    public bool CanGoNext => CurrentStepIndex is not 3 || AtLeastOneGranted;

    public UserControl CurrentStepView => _steps[CurrentStepIndex];

    public string StepPreviewSource =>
        CurrentStepIndex switch
        {
            0 or 1 or 2 => "avares://FileTidy.GUI/Assets/Images/onboarding/welcome.png",
            3 => $"avares://FileTidy.GUI/Assets/Images/onboarding/{GetAccessImageByOs()}",
            4 => "avares://FileTidy.GUI/Assets/Images/onboarding/completion.png",
            _ => string.Empty
        };

    public Bitmap StepPreviewBinding => ImageHelper.LoadFromResource(new Uri(StepPreviewSource));

    public HorizontalAlignment PreviewImageHorizontalAlignment =>
        CurrentStepIndex is 0 or 1 or 2 ? HorizontalAlignment.Right : HorizontalAlignment.Center;

    public Thickness PreviewContainerMargin =>
        CurrentStepIndex is 0 or 1 or 2 ? new Thickness(24, 0, -200, 0) : new Thickness(0);

    public string PrimaryButtonText => CurrentStepIndex switch
    {
        0 => "Get Started",
        1 => "Next",
        2 => "Next",
        3 => "Continue",
        4 => "Open FileTidy",
        _ => "Next"
    };

    public string StepTitle => CurrentStepIndex switch
    {
        0 => "Welcome to FileTidy",
        1 => "Our Pledge to Privacy",
        2 => "Data Security & Encryption",
        3 => "Folder Access Permissions",
        4 => "You're Ready",
        _ => string.Empty
    };

    // =========================
    // Constructor
    // =========================
    public OnboardingWindowViewModel(
        IFileOperationStore fileOperationStore,
        IFolderService folderService,
        IAppConfigService appConfig,
        IDeviceTelemetryService deviceTelemetryService)
    {
        _fileOperationStore = fileOperationStore;
        _folderService = folderService;
        _appConfig = appConfig;
        _deviceTelemetryService = deviceTelemetryService;

        _steps =
        [
            new WelcomeStepView(),
            new PledgeStepView(),
            new SecurityStepView(),
            new AccessStepView(),
            new CompletionStepView(),
        ];

        _ = ProbeAllAsync();
    }

    public OnboardingWindowViewModel()
    {
        _steps = [ new WelcomeStepView() ];
        _ = ProbeAllAsync();
    }

    // =========================
    // Commands
    // =========================
    [RelayCommand]
    private async Task ProbeAllAsync()
    {
        DesktopGranted   = await _folderService.CanAccessAsync(DesktopPath);
        DownloadsGranted = await _folderService.CanAccessAsync(DownloadsPath);
        DocumentsGranted = await _folderService.CanAccessAsync(DocumentsPath);
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void PrimaryAction()
    {
        if (CurrentStepIndex == StepsCountMinusOne)
        {
            _ = Telemetry.LogAsync(TelemetryEventTypes.PermissionsGranted);

            App.LaunchMainWindow();
            
            return;
        }

        NextStep();
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStepIndex < _steps.Count - 1)
        {
            CurrentStepIndex++;
            OnStepChanged();
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
            CurrentStepIndex--;
    }

    [RelayCommand]
    private async Task OpenOsSettingsAsync()
    {
        await _folderService.OpenSystemFilesAndFoldersSettingsAsync();

        await ProbeAllAsync();
    }

    // =========================
    // Private Helpers
    // =========================
    private string GetAccessImageByOs()
    {
        if (OperatingSystem.IsMacOS())   return "access-macos.png";
        if (OperatingSystem.IsWindows()) return "access-windows.png";
        return "access-linux.png";
    }

    private CancellationTokenSource? _accessStepCts;

    private async Task MonitorAccessStepAsync()
    {
        _accessStepCts?.Cancel();
        _accessStepCts = new CancellationTokenSource();
        var token = _accessStepCts.Token;

        try
        {
            while (!token.IsCancellationRequested && CurrentStepIndex == 3)
            {
                await ProbeAllAsync();
                await Task.Delay(1500, token);
            }
        }
        catch (TaskCanceledException) { }
    }

    private void OnStepChanged()
    {
        if (CurrentStepIndex == 3)
        {
            _ = ProbeAllAsync();
            _ = MonitorAccessStepAsync();
        }
        else
        {
            _accessStepCts?.Cancel();
        }
        
        if (CurrentStepIndex == StepsCountMinusOne)
        {
            _ = CompleteOnboarding();
        }
    }
    
    private async Task CompleteOnboarding()
    {
        _ = _appConfig.SetHasCompletedOnboardingAsync(true);

        var deviceId = await _appConfig.GetDeviceIdAsync();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            await _appConfig.SetDeviceIdAsync(deviceId);
        }
        
        _ = Task.Run(async () =>
        {
            Console.WriteLine("Calling LinkAsync");
            await _deviceTelemetryService.LinkAsync(deviceId);
            _ = _deviceTelemetryService.LogEventAsync(deviceId, TelemetryEventTypes.OnboardingComplete,
                new { version = SystemInfoHelper.GetAppVersion() });

        });
    }

}