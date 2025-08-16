// File: Services/DeviceTelemetryService.cs

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Helpers;
using Microsoft.Extensions.Configuration;

namespace FileTidy.GUI.Services;

public sealed class DeviceTelemetryService : IDeviceTelemetryService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public DeviceTelemetryService(IConfiguration config)
    {
        _config = config;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<bool> LinkAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!TryGetSupabase(out var baseUrl, out var anonKey))
            return false;

        var url = $"{baseUrl.TrimEnd('/')}/rest/v1/rpc/link_device";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthHeaders(req, anonKey);
        // RPCs don’t need Accept-Profile/Content-Profile
        req.Headers.Add("Prefer", "return=minimal");

        var payload = new
        {
            p_device_id = deviceId,
            p_os = SystemInfoHelper.GetOsName(),
            p_app_version = SystemInfoHelper.GetAppVersion()
        };
        req.Content = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode) return true;

        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        return false;
    }

    public async Task<bool> LogEventAsync(string deviceId, string eventType, object? meta = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("eventType is required", nameof(eventType));

        if (!TryGetSupabase(out var baseUrl, out var anonKey))
            return false;

        var url = $"{baseUrl.TrimEnd('/')}/rest/v1/rpc/log_device_activity";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthHeaders(req, anonKey);
        req.Headers.Add("Prefer", "return=minimal");

        var payload = new
        {
            p_device_id = deviceId,
            p_event_type = eventType,
            p_meta = meta
        };
        req.Content = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode) return true;

        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        return false;
    }

    // ---- helpers ----

    private bool TryGetSupabase(out string url, out string key)
    {
        url = _config["Cloud:SupabaseUrl"] ?? "";
        key = _config["Cloud:SupabaseAnonKey"] ?? "";
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }
        return true;
    }

    private static void AddAuthHeaders(HttpRequestMessage req, string anonKey)
    {
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", anonKey);
        req.Headers.TryAddWithoutValidation("apikey", anonKey);
    }
}