namespace FileTidy.GUI.Constants;

public static class TelemetryEventTypes
{
    public const string AppOpen = "app_open";
    public const string OnboardingComplete = "onboarding_complete";
    public const string PermissionsOpenSettings = "permissions_open_settings";
    public const string PermissionsGranted = "permissions_granted";
    public const string PermissionsRevoked = "permissions_revoked";
    public const string SettingsOpen = "settings_open";
    public const string SettingsSaved = "settings_saved";
    public const string HelpOpen = "help_open";
    public const string SortStart = "sort_start";
    public const string SortComplete = "sort_complete";
    public const string SortCancel = "sort_cancel";
    public const string SortError = "sort_error";
    public const string RevertLastSortSession = "revert_last_sort_session";
    public const string RevertFile = "revert_file";
    public const string RevertFiles = "revert_files";
    
    public const string GlobalError = "global_error";
    public const string AppCrash    = "app_crash"; 
}