using System;
using System.Threading.Tasks;
using FileTidy.GUI.Constants;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FileTidy.GUI.Helpers;

public static class Telemetry
{
    private static IServiceProvider ServiceProvider => App.Services;

    public static async Task LogAsync(string eventType, object? meta = null)
    {
        try
        {
            var appConfigService = ServiceProvider.GetRequiredService<IAppConfigService>();
            var deviceTelemetryService = ServiceProvider.GetRequiredService<IDeviceTelemetryService>();

            var deviceId = await appConfigService.GetDeviceIdAsync();
            if (string.IsNullOrWhiteSpace(deviceId))
                return;

            _ = deviceTelemetryService.LogEventAsync(deviceId, eventType, meta);
        }
        catch
        {
            // swallow — telemetry must never break UX
        }
    }

    /// <summary>
    /// Logs permissions_granted / permissions_revoked when a bool flips.
    /// </summary>
    public static async Task TrackAccessFlipAsync(string folderKey, bool previousValue, bool currentValue)
    {
        if (previousValue == currentValue) return;

        var eventType = currentValue 
            ? TelemetryEventTypes.PermissionsGranted
            : TelemetryEventTypes.PermissionsRevoked;

        await LogAsync(eventType, new { folder = folderKey });
    }
}