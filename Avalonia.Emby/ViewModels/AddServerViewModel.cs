using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using ReactiveUI;
using System.Text.Json.Serialization;

namespace Avalonia.Emby.ViewModels;

public class AddServerViewModel : ViewModelBase
{
    private string _username;
    private string _password;
    private string _serverUrl;
    private const string ClientName = "Avalonia.Emby";
    private const string DeviceName = "Desktop";
    private const string Version = "1.0.0.0";
    private readonly string _deviceId = Guid.NewGuid().ToString();
    private readonly HttpClient _httpClient = new HttpClient();

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

                // Validate inputs
                if (string.IsNullOrWhiteSpace(ServerUrl) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    return;
                }

                // Format server URL
                var baseUrl = ServerUrl.Trim();
                if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = "https://" + baseUrl; // Default to HTTPS for security
                }
                baseUrl = baseUrl.TrimEnd('/');

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
                Console.WriteLine($"Auth Response: {authJson}"); // Add debug logging

                var authResult = JsonSerializer.Deserialize<AuthResponse>(authJson);
                var serverName = authResult.User.ServerName;

                // Debug prints for server name and access token
                Console.WriteLine($"Server Name: {serverName}");
                Console.WriteLine($"Access Token: {authResult.AccessToken}");

                // Create server info
                var server = new ServerViewModel(
                    serverName,
                    baseUrl,
                    Username,
                    authResult.AccessToken
                );

                Console.WriteLine($"Connected to {server.Name}");
                ServerList.Add(server); // Add to ServerList
                AddServer = server;

                // Close the dialog
                window?.Close();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Connection error: {ex}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Connection error: Request timed out");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex}");
            }
        });

        CloseWindowCommand = ReactiveCommand.Create<Window>(window =>
        {
            window?.Close();
        });
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

    private class UserInfo
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("ServerName")]
        public string ServerName { get; set; } = "Emby Server";
    }
}