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
using EmbyClient.Dotnet.Api;
using EmbyClient.Dotnet.Client;
using EmbyClient.Dotnet.Model;

namespace Avalonia.Emby.ViewModels;

public class AddAccountViewModel : ViewModelBase
{
    private string _username;
    private string _password;
    private string _serverUrl;
    private string _serverName;
    private bool _isConnecting;
    public const string ClientName = "Tsukimi";
    public const string Version = "0.21.0";
    public static string DeviceId = Guid.NewGuid().ToString();
    private readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };
    private Configuration apiConfiguration;
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
        AddAccountCommand = ReactiveCommand.CreateFromTask<Window>(async (window) => await AddAccount(window));
        CloseWindowCommand = ReactiveCommand.Create<Window>(window =>
        {
            window?.Close();
        });
    }

    private async Task AddAccount(Window window)
    {
        try
        {
            IsConnecting = true;

            if (!await checkUrlValid(window)) return;
            if (!await checkUsernameValid(window)) return;

            var baseUrl = formatUrl();

            var newConfig = new Configuration();
            newConfig.BasePath = baseUrl;
            
            newConfig.UserAgent = $"{ClientName}/{Version}";
            newConfig.Timeout = 5000;
            var Config = await getAuth(newConfig);

            var serverInfo = await getServerInfo(Config.config);

            var serverName = ServerName ?? serverInfo.ServerName ?? "Emby Server";

            // Create account info
            var account = new Account(
                serverName,
                baseUrl,
                Config.result.User.Id,
                Username,
                Password,
                Config.result.AccessToken
            );

            IsConnecting = false;
            window?.Close(account);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            if (message.StartsWith("Error calling PostUsersAuthenticatebyname:"))
            {
                message = message.Split(':', 2)[1].Trim();
            }
            await flyoutBox($"Error: {message}", window);
            IsConnecting = false;
        }
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

    private async Task<(Configuration config, AuthenticationAuthenticationResult result)> getAuth(Configuration config)
    {
        var newConfig = config;

        var authData = new AuthenticateUserByName(Username, Password);
        var userClient = new UserServiceApi(newConfig);

        var authResult = await userClient.PostUsersAuthenticatebynameAsync(authData, getTempAuthHeader());
        var authHeader = getAuthHeader(authResult);
        newConfig.DefaultHeader.Add("X-Emby-Authorization", authHeader);

        return (newConfig, authResult);
    }

    private async Task<SystemInfo> getServerInfo(Configuration config)
    {
        var systemService = new SystemServiceApi(config);
        var info = await systemService.GetSystemInfoAsync();

        return info;
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

    private string getAuthHeader(AuthenticationAuthenticationResult authResult)
    {
        return $"Emby UserId=\"{authResult.User.Id}\", Client=\"{authResult.SessionInfo._Client}\", Device=\"{authResult.SessionInfo.DeviceName}\", DeviceId=\"{authResult.SessionInfo.DeviceId}\", Version=\"{Version}\", Token=\"{authResult.AccessToken}\"";
    }
}