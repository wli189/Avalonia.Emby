using System;
using System.Windows.Input;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using Avalonia.Emby.Models;

namespace Avalonia.Emby.ViewModels;

public class AddAccountViewModel : ViewModelBase
{
    private string _username;
    private string _password;
    private string _serverUrl;
    private string _serverName;
    private bool _isConnecting;
    private readonly EmbyAuthenticationService _authService;

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
        _authService = new EmbyAuthenticationService();

        AddAccountCommand = ReactiveCommand.CreateFromTask<Window>(AddAccountAsync);
        CloseWindowCommand = ReactiveCommand.Create<Window>(window => window?.Close());
    }

    private async Task AddAccountAsync(Window window)
    {
        try
        {
            IsConnecting = true;

            if (!await ValidateInput(window)) return;

            var baseUrl = _authService.FormatServerUrl(ServerUrl);
            var authResult = await _authService.AuthenticateAsync(baseUrl, Username, Password);
            var serverInfo = await _authService.GetServerInfoAsync(baseUrl, authResult);

            var serverName = ServerName ?? serverInfo.ServerName ?? "Emby Server";
            var account = new Account(
                serverName,
                baseUrl,
                authResult.SessionInfo.UserId,
                Username,
                Password,
                authResult.AccessToken
            );

            window?.Close(account);
        }
        catch (HttpRequestException ex)
        {
            var message = ex.StatusCode != null
                ? $"Connection error: {(int)ex.StatusCode} - {ex.Message}"
                : $"Connection error: {ex.Message}";
            await UIHelper.ShowFlyoutMessage(message, window);
        }
        catch (TaskCanceledException)
        {
            await UIHelper.ShowFlyoutMessage("Connection timed out", window);
        }
        catch (Exception ex)
        {
            await UIHelper.ShowFlyoutMessage($"Error: {ex.Message}", window);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private async Task<bool> ValidateInput(Window window)
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            await UIHelper.ShowFlyoutMessage("Please enter a server URL", window);
            return false;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            await UIHelper.ShowFlyoutMessage("Please enter a username", window);
            return false;
        }

        return true;
    }
}