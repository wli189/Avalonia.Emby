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
using Avalonia.Media;
using Avalonia.Styling;
using Emby.ApiClient.Model;

namespace Avalonia.Emby.ViewModels;

public class AddServerViewModel : ViewModelBase
{
    private string _username;
    private string _password;
    private string _serverUrl;
    private string _serverName;
    private bool _isConnecting;
    private const string ClientName = "Avalonia.Emby";
    private const string DeviceName = "Desktop";
    private const string Version = "1.0.0.0";
    private readonly string _deviceId = Guid.NewGuid().ToString();
    private readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };

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
        set
        {
            this.RaiseAndSetIfChanged(ref _serverName, value);
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
    }

    private ServerViewModel? _addServer;

    public ObservableCollection<ServerViewModel> ServerList { get; } = new();

    public ServerViewModel? AddServer
    {
        get => _addServer;
        set => this.RaiseAndSetIfChanged(ref _addServer, value);
    }

    public ICommand ConnectServerCommand { get; }
    public ICommand CloseWindowCommand { get; }

    public AddServerViewModel()
    {

        ConnectServerCommand = ReactiveCommand.CreateFromTask<Window>(async (window) =>
        {
            try
            {
                IsConnecting = true;

                // Validate inputs
                if (string.IsNullOrWhiteSpace(ServerUrl))
                {
                    await flyoutBox("Please enter a server URL", window);
                    IsConnecting = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(Username))
                {
                    await flyoutBox("Please enter a username", window);
                    IsConnecting = false;
                    return;
                }

                var baseUrl = formatUrl(ServerUrl);

                var authResult = await getAuth(baseUrl);

                var serverInfo = await getServerInfo(baseUrl, authResult.AccessToken);

                var serverName = ServerName ?? serverInfo.ServerName ?? "Emby Server";

                // Create server info
                var server = new ServerViewModel(
                    serverName,
                    baseUrl,
                    Username,
                    authResult.AccessToken
                );

                // Console.WriteLine($"Server: Name={server.Name}, URL={server.Url}, Username={server.Username}, AccessToken={server.AccessToken}");
                // Console.WriteLine($"Connected to {server.Name}");
                ServerList.Add(server); // Add to ServerList
                AddServer = server;

                // Close the dialog
                IsConnecting = false;
                window?.Close();
            }
            catch (HttpRequestException ex)
            {
                var message = ex.StatusCode != null
                    ? $"Connection error: {(int)ex.StatusCode} - {ex.Message}"
                    : $"Connection error: {ex.Message}";
                await flyoutBox(message, window);
            }
            catch (TaskCanceledException)
            {
                await flyoutBox("Connection timed out", window);
            }
            catch (Exception ex)
            {
                await flyoutBox($"Error: {ex.Message}", window);
            }
        });

        CloseWindowCommand = ReactiveCommand.Create<Window>(window =>
        {
            window?.Close();
        });
    }

    private string formatUrl(string url)
    {
        // Format server URL
        var baseUrl = ServerUrl.Trim();
        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://" + baseUrl; // Default to HTTPS for security
        }
        baseUrl = baseUrl.TrimEnd('/');

        // Add default ports if not specified
        var uri = new Uri(baseUrl);
        if (!baseUrl.Contains(":", StringComparison.OrdinalIgnoreCase))
        {
            // No port specified, add default port
            var port = uri.Scheme.ToLower() == "http" ? "80" : "443";
            baseUrl = $"{baseUrl}:{port}";
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
            Content = authContent
        };

        // Add the X-Emby-Authorization header with the correct format
        authRequest.Headers.Add("X-Emby-Authorization",
            $"Emby UserId=\"\", Client=\"{ClientName}\", Device=\"{DeviceName}\", DeviceId=\"{_deviceId}\", Version=\"{Version}\"");

        var authResponse = await _httpClient.SendAsync(authRequest);
        authResponse.EnsureSuccessStatusCode();

        var authJson = await authResponse.Content.ReadAsStringAsync();

        var authResult = JsonSerializer.Deserialize<AuthResponse>(authJson);

        return authResult!;
    }

    private async Task<ServerInfo> getServerInfo(string baseUrl, string accessToken)
    {


        var serverInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/System/Info")
        {
            Headers =
            {
                { "X-Emby-Authorization", $"Emby UserId=\"\", Client=\"{ClientName}\", Device=\"{DeviceName}\", DeviceId=\"{_deviceId}\", Version=\"{Version}\", Token=\"{accessToken}\"" }
            }
        };

        var serverInfoResponse = await _httpClient.SendAsync(serverInfoRequest);
        serverInfoResponse.EnsureSuccessStatusCode();

        var serverInfoJson = await serverInfoResponse.Content.ReadAsStringAsync();
        var serverInfo = JsonSerializer.Deserialize<ServerInfo>(serverInfoJson);
        return serverInfo!;
    }


    private class AuthResponse
    {
        [JsonPropertyName("AccessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("ServerId")]
        public string ServerId { get; set; } = string.Empty;

        [JsonPropertyName("User")]
        public UserInfo User { get; set; } = new();
    }

    private class ServerInfo
    {
        [JsonPropertyName("ServerName")]
        public string ServerName { get; set; } = string.Empty;
    }

    private class UserInfo
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;
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
        
        flyout.ShowAt(window);
        await Task.Delay(2000);
        flyout.Hide();
    }
}