using System.Threading;
using System.Threading.Tasks;

namespace FileTidy.GUI.Contracts;

public interface IDeviceTelemetryService
{
    Task<bool> LinkAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<bool> LogEventAsync(string deviceId, string eventType, object? meta = null, CancellationToken cancellationToken = default);
}