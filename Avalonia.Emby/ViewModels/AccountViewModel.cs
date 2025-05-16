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

public class AccountViewModel : ViewModelBase
{
    private readonly Account _account;
    private readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };
    private bool _isConnecting;
    private readonly string _deviceId = Guid.NewGuid().ToString();

    public AccountViewModel(Account account)
    {
        _account = account;
        _httpClient = new HttpClient();
        ConnectServerCommand = ReactiveCommand.CreateFromTask<Window>(ConnectToServerAsync);
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
    }

    public ICommand ConnectServerCommand { get; }

    public Account Account => _account;
    public string ServerName => _account.ServerName;
    public string ServerUrl => _account.ServerUrl;
    public string UserId => _account.UserId;
    public string Username => _account.Username;
    public string Password => _account.Password;
    public string AccessToken => _account.AccessToken;

    private async Task ConnectToServerAsync(Window window)
    {
        try
        {
            IsConnecting = true;

            // Verify server is accessible
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/System/Info");
            request.Headers.Add("X-Emby-Authorization", $"Emby UserId=\"\", Client=\"{AddAccountViewModel.ClientName}\", Device=\"{AddAccountViewModel.DeviceName}\", DeviceId=\"{_deviceId}\", Version=\"{AddAccountViewModel.Version}\", Token=\"{AccessToken}\"");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Navigate to library window
            var libraryWindow = new Views.LibraryWindow
            {
                DataContext = new LibraryWindowViewModel(_account)
            };
            libraryWindow.Show();
            window.Close();


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