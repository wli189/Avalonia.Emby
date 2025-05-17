using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Platform;
using ReactiveUI;
using System.Text.Json.Serialization;
using Avalonia.Animation;
using Avalonia.Emby.Models;
using Avalonia.Media;
using Avalonia.Styling;
using Emby.ApiClient.Model;

namespace Avalonia.Emby.ViewModels;

public class AddAccountViewModel : ViewModelBase
{
    private string _username;
    private string _password;
    private string _serverUrl;
    private string _serverName;
    private bool _isConnecting;
    public const string ClientName = "Tsukimi";
    public const string DeviceName = "Desktop";
    public const string Version = "0.21.0";
    private readonly string DeviceId = Guid.NewGuid().ToString();
    private readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };
    public ICommand AddAccountCommand { get; }
    public ICommand CloseWindowCommand { get; }

    public string ServerUrl
    {
        get => _serverUrl;
        set => this.RaiseAndSetIfChanged(ref _serverUrl, value);
    }

    public string Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public string ServerName
    {
        get => _serverName;
        set => this.RaiseAndSetIfChanged(ref _serverName, value);
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
    }

    public AddAccountViewModel()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{ClientName}/{Version}");

        AddAccountCommand = ReactiveCommand.CreateFromTask<Window>(async (window) =>
        {
            try
            {
                IsConnecting = true;

                if (!await checkUrlValid(window)) return;
                if (!await checkUsernameValid(window)) return;

                var baseUrl = formatUrl();

                var authResult = await getAuth(baseUrl);

                var UserId = authResult.SessionInfo.UserId;

                var serverInfo = await getServerInfo(baseUrl, authResult);

                var serverName = ServerName ?? serverInfo.ServerName ?? "Emby Server";

                // Create account info
                var account = new Account(
                    serverName,
                    baseUrl,
                    UserId,
                    Username,
                    Password,
                    authResult.AccessToken
                );

                IsConnecting = false;
                window?.Close(account);
            }
            catch (HttpRequestException ex)
            {
                var message = ex.StatusCode != null
                    ? $"Connection error: {(int)ex.StatusCode} - {ex.Message}"
                    : $"Connection error: {ex.Message}";
                await flyoutBox(message, window);
                IsConnecting = false;
            }
            catch (TaskCanceledException)
            {
                await flyoutBox("Connection timed out", window);
                IsConnecting = false;
            }
            catch (Exception ex)
            {
                await flyoutBox($"Error: {ex.Message}", window);
                IsConnecting = false;
            }
        });

        CloseWindowCommand = ReactiveCommand.Create<Window>(window =>
        {
            window?.Close();
        });
    }

    private async Task<bool> checkUsernameValid(Window window)
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            await flyoutBox("Please enter a username", window);
            IsConnecting = false;
            return false;
        }
        return true;
    }

    private async Task<bool> checkUrlValid(Window window)
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            await flyoutBox("Please enter a server URL", window);
            IsConnecting = false;
            return false;
        }
        return true;
    }

    private string formatUrl()
    {
        // Format server URL
        var baseUrl = ServerUrl.Trim();
        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://" + baseUrl; // Default to HTTPS for security
        }

        // Parse the URL and check if port is specified
        var uri = new Uri(baseUrl);
        if (uri.IsDefaultPort)
        {
            // No port specified or using default port, add explicit port
            var port = uri.Scheme.ToLower() == "http" ? "80" : "443";
            baseUrl = $"{uri.Scheme}://{uri.Host}:{port}";
        }

        return baseUrl;
    }

    private async Task<AuthResponse> getAuth(string baseUrl)
    {
        // Authenticate
        var authData = new
        {
            Username = Username,
            Pw = Password
        };

        var authContent = new StringContent(
            JsonSerializer.Serialize(authData),
            Encoding.UTF8,
            "application/json");

        var authRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/Users/AuthenticateByName")
        {
            Content = authContent,
            Headers = { { "X-Emby-Authorization", getTempAuthHeader() } }
        };

        var authResponse = await _httpClient.SendAsync(authRequest);
        authResponse.EnsureSuccessStatusCode();

        var authJson = await authResponse.Content.ReadAsStringAsync();

        var authResult = JsonSerializer.Deserialize<AuthResponse>(authJson);

        return authResult!;
    }

    private async Task<ServerInfo> getServerInfo(string baseUrl, AuthResponse authResult)
    {
        var serverInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/System/Info")
        { Headers = { { "X-Emby-Authorization", getAuthHeader(authResult) } } };

        var serverInfoResponse = await _httpClient.SendAsync(serverInfoRequest);
        serverInfoResponse.EnsureSuccessStatusCode();

        var serverInfoJson = await serverInfoResponse.Content.ReadAsStringAsync();
        var serverInfo = JsonSerializer.Deserialize<ServerInfo>(serverInfoJson);
        return serverInfo!;
    }

    private async Task flyoutBox(string message, Window window)
    {
        var flyout = new Flyout
        {
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300,
            },
            Placement = PlacementMode.Bottom,
            ShowMode = FlyoutShowMode.Transient,
            VerticalOffset = 5,
        };

        var content = (TextBlock)flyout.Content;
        content.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = TextBlock.OpacityProperty,
                Duration = TimeSpan.FromSeconds(0.2)
            }
        };

        content.Opacity = 0;
        flyout.ShowAt(window);
        content.Opacity = 1;
        await Task.Delay(1000);
        content.Opacity = 0;
        await Task.Delay(200);
        flyout.Hide();
    }

    private string getTempAuthHeader()
    {
        return $"Emby UserId=\"\", Client=\"{ClientName}\", Device=\"{Environment.MachineName}\", DeviceId=\"{DeviceId}\", Version=\"{Version}\", Token=\"\"";
    }

    private string getAuthHeader(AuthResponse authResult)
    {
        return $"Emby UserId=\"{authResult.User.Id}\", Client=\"{authResult.SessionInfo.Client}\", Device=\"{authResult.SessionInfo.DeviceName}\", DeviceId=\"{authResult.SessionInfo.DeviceId}\", Version=\"{Version}\", Token=\"{authResult.AccessToken}\"";
    }
}