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
using EmbyClient.Dotnet.Api;
using EmbyClient.Dotnet.Client;

namespace Avalonia.Emby.ViewModels;

public class AccountViewModel : ViewModelBase
{
    private readonly Account _account;
    public Account Account => _account;
    public string ServerName => _account.ServerName;
    public string ServerUrl => _account.ServerUrl;
    public string UserId => _account.UserId;
    public string Username => _account.Username;
    public string Password => _account.Password;
    public string AccessToken => _account.AccessToken;
    private bool _isConnecting;
    public event EventHandler<Account>? AccountDeleted;
    public Interaction<AddAccountViewModel, Account?> ShowDialog { get; } = new();
    public ICommand ConnectServerCommand { get; }
    public ICommand DeleteAccountCommand { get; }
    public ICommand EditAccountCommand { get; }

    public AccountViewModel(Account account)
    {
        _account = account;
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

            // Setup API configuration
            var config = new Configuration
            {
                BasePath = ServerUrl,
                UserAgent = $"{AddAccountViewModel.ClientName}/{AddAccountViewModel.Version}",
                Timeout = 5000
            };

            // Add auth header
            var authHeader = $"Emby UserId=\"{UserId}\", Client=\"{AddAccountViewModel.ClientName}\", Device=\"{Environment.MachineName}\", DeviceId=\"{AddAccountViewModel.DeviceId}\", Version=\"{AddAccountViewModel.Version}\", Token=\"{AccessToken}\"";
            config.DefaultHeader.Add("X-Emby-Authorization", authHeader);

            // Try to connect and get server info
            var systemService = new SystemServiceApi(config);
            await systemService.GetSystemInfoAsync();

            // Navigate to library window
            var libraryWindow = new Views.LibraryWindow
            {
                DataContext = new LibraryWindowViewModel(_account)
            };
            libraryWindow.Show();
            window.Close();

            IsConnecting = false;
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            if (message.StartsWith("Error calling GetSystemInfo:"))
            {
                message = message.Split(':', 2)[1].Trim();
            }
            await flyoutBox($"Error: {message}", window);
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