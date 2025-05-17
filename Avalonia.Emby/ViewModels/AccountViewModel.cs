using System;
using Avalonia.Emby.Models;
using System.Windows.Input;
using ReactiveUI;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using System.Reactive;
using System.Reactive.Linq;

namespace Avalonia.Emby.ViewModels;

public class AccountViewModel : ViewModelBase
{
    private readonly Account _account;
    private readonly EmbyAuthenticationService _authService;
    private bool _isConnecting;
    public event EventHandler<Account>? AccountDeleted;
    public Interaction<AddAccountViewModel, Account?> ShowDialog { get; } = new();
    public ICommand ConnectServerCommand { get; }
    public ICommand DeleteAccountCommand { get; }
    public ICommand EditAccountCommand { get; }

    public Account Account => _account;
    public string ServerName => _account.ServerName;
    public string ServerUrl => _account.ServerUrl;
    public string UserId => _account.UserId;
    public string Username => _account.Username;
    public string Password => _account.Password;
    public string AccessToken => _account.AccessToken;

    public AccountViewModel(Account account)
    {
        _account = account;
        _authService = new EmbyAuthenticationService();

        ConnectServerCommand = ReactiveCommand.CreateFromTask<Window>(ConnectToServerAsync);
        DeleteAccountCommand = ReactiveCommand.Create<Window>(DeleteAccount);
        EditAccountCommand = ReactiveCommand.CreateFromTask<Window>(async window => await EditAccount());
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
    }

    private void DeleteAccount(Window window)
    {
        AccountDeleted?.Invoke(this, _account);
    }

    private async Task EditAccount()
    {
        var viewModel = new AddAccountViewModel
        {
            ServerName = ServerName,
            ServerUrl = ServerUrl,
            Username = Username,
            Password = Password
        };
        var result = await ShowDialog.Handle(viewModel);
        if (result != null)
        {
            _account.ServerName = result.ServerName;
            _account.ServerUrl = result.ServerUrl;
            _account.Username = result.Username;
            _account.Password = result.Password;
            this.RaisePropertyChanged(nameof(ServerName));
            this.RaisePropertyChanged(nameof(ServerUrl));
            this.RaisePropertyChanged(nameof(Username));
            this.RaisePropertyChanged(nameof(Password));
        }
    }

    private async Task ConnectToServerAsync(Window window)
    {
        try
        {
            IsConnecting = true;

            // Verify server is accessible
            var serverInfo = await _authService.GetServerInfoAsync(ServerUrl, new AuthResponse
            {
                AccessToken = AccessToken,
                User = new UserDto { Id = UserId },
                SessionInfo = new SessionInfo
                {
                    UserId = UserId,
                    Client = EmbyAuthenticationService.ClientName,
                    DeviceName = EmbyAuthenticationService.DeviceName,
                    DeviceId = EmbyAuthenticationService.DeviceId,
                }
            });

            // Navigate to library window
            var libraryWindow = new Views.LibraryWindow
            {
                DataContext = new LibraryWindowViewModel(_account)
            };
            libraryWindow.Show();
            window.Close();
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
}