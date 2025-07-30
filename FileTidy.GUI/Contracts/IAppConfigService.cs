using System;
using System.Threading.Tasks;

namespace FileTidy.GUI.Contracts;

public interface IAppConfigService
{
    Task<bool> GetHasCompletedOnboardingAsync();
    Task SetHasCompletedOnboardingAsync(bool value);

    Task<Guid?> GetLastSortSessionIdAsync();
    Task SetLastSortSessionIdAsync(Guid sessionId);

    Task<bool> GetDarkModeEnabledAsync();
    Task SetDarkModeEnabledAsync(bool value);

    Task<string?> GetDeviceIdAsync();
    Task SetDeviceIdAsync(string deviceId);

    Task DeleteConfigAsync(string key);
}
