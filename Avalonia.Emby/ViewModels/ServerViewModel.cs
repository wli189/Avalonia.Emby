using System;
using Avalonia.Emby.Models;
using System.Windows.Input;
using ReactiveUI;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Emby.ViewModels;

public class ServerViewModel : ViewModelBase
{
    private readonly Server _server;
    private readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };
    private bool _isConnecting;

    public ServerViewModel(Server server)
    {
        _server = server;
        _httpClient = new HttpClient();
        ConnectServerCommand = ReactiveCommand.CreateFromTask<Window>(ConnectToServerAsync);
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
    }

    public ICommand ConnectServerCommand { get; }

    public Server Server => _server;
    public string ServerName => _server.ServerName;
    public string ServerUrl => _server.ServerUrl;
    public string UserId => _server.UserId;
    public string Username => _server.Username;
    public string Password => _server.Password;
    public string AccessToken => _server.AccessToken;

    private async Task ConnectToServerAsync(Window window)
    {
        try
        {
            IsConnecting = true;

            // Verify server is accessible
            var response = await _httpClient.GetAsync($"{ServerUrl}/System/Info");
            response.EnsureSuccessStatusCode();

            // TODO: Navigate to server content view
            // This will be implemented when we add the server content view

            IsConnecting = false;
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
}