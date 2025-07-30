using System;
using System.Threading.Tasks;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.Services;

public class AppConfigService : IAppConfigService
{
    private readonly IFileOperationStore _store;

    public AppConfigService(IFileOperationStore store)
    {
        _store = store;
    }

    public async Task<bool> GetHasCompletedOnboardingAsync()
    {
        var value = await _store.GetConfigValueAsync(AppConfigKeys.HasCompletedOnboarding);
        return bool.TryParse(value, out var result) && result;
    }

    public Task SetHasCompletedOnboardingAsync(bool value) =>
        _store.SaveConfigValueAsync(AppConfigKeys.HasCompletedOnboarding, value.ToString());

    public async Task<Guid?> GetLastSortSessionIdAsync()
    {
        var value = await _store.GetConfigValueAsync(AppConfigKeys.LastSortSessionId);
        return Guid.TryParse(value, out var result) ? result : (Guid?)null;
    }

    public Task SetLastSortSessionIdAsync(Guid sessionId) =>
        _store.SaveConfigValueAsync(AppConfigKeys.LastSortSessionId, sessionId.ToString());

    public async Task<bool> GetDarkModeEnabledAsync()
    {
        var value = await _store.GetConfigValueAsync(AppConfigKeys.DarkModeEnabled);
        return bool.TryParse(value, out var result) && result;
    }

    public Task SetDarkModeEnabledAsync(bool value) =>
        _store.SaveConfigValueAsync(AppConfigKeys.DarkModeEnabled, value.ToString());

    public Task<string?> GetDeviceIdAsync() =>
        _store.GetConfigValueAsync(AppConfigKeys.DeviceId);

    public Task SetDeviceIdAsync(string deviceId) =>
        _store.SaveConfigValueAsync(AppConfigKeys.DeviceId, deviceId);

    public Task DeleteConfigAsync(string key) =>
        _store.DeleteConfigValueAsync(key);
}
