using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Avalonia.Emby.Models;

public class EmbyAuthenticationService
{
    private readonly HttpClient _httpClient;
    public const string ClientName = "Tsukimi";
    public const string DeviceName = "Desktop";
    public const string Version = "0.21.0";
    private readonly string DeviceId = Guid.NewGuid().ToString();

    public EmbyAuthenticationService()
    {
        _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{ClientName}/{Version}");
    }

    public string FormatServerUrl(string serverUrl)
    {
        var baseUrl = serverUrl.Trim();
        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://" + baseUrl;
        }

        var uri = new Uri(baseUrl);
        if (uri.IsDefaultPort)
        {
            var port = uri.Scheme.ToLower() == "http" ? "80" : "443";
            baseUrl = $"{uri.Scheme}://{uri.Host}:{port}";
        }

        return baseUrl;
    }

    public async Task<AuthResponse> AuthenticateAsync(string baseUrl, string username, string password)
    {
        var authData = new
        {
            Username = username,
            Pw = password
        };

        var authContent = new StringContent(
            JsonSerializer.Serialize(authData),
            Encoding.UTF8,
            "application/json");

        var authRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/Users/AuthenticateByName")
        {
            Content = authContent,
            Headers = { { "X-Emby-Authorization", GetTempAuthHeader() } }
        };

        var authResponse = await _httpClient.SendAsync(authRequest);
        authResponse.EnsureSuccessStatusCode();

        var authJson = await authResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(authJson)!;
    }

    public async Task<ServerInfo> GetServerInfoAsync(string baseUrl, AuthResponse authResult)
    {
        var serverInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/System/Info")
        { Headers = { { "X-Emby-Authorization", GetAuthHeader(authResult) } } };

        var serverInfoResponse = await _httpClient.SendAsync(serverInfoRequest);
        serverInfoResponse.EnsureSuccessStatusCode();

        var serverInfoJson = await serverInfoResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ServerInfo>(serverInfoJson)!;
    }

    private string GetTempAuthHeader()
    {
        return $"Emby UserId=\"\", Client=\"{ClientName}\", Device=\"{Environment.MachineName}\", DeviceId=\"{DeviceId}\", Version=\"{Version}\", Token=\"\"";
    }

    private string GetAuthHeader(AuthResponse authResult)
    {
        return $"Emby UserId=\"{authResult.User.Id}\", Client=\"{authResult.SessionInfo.Client}\", Device=\"{authResult.SessionInfo.DeviceName}\", DeviceId=\"{authResult.SessionInfo.DeviceId}\", Version=\"{Version}\", Token=\"{authResult.AccessToken}\"";
    }
}