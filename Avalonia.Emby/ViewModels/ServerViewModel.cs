using Avalonia.Emby.Models;

namespace Avalonia.Emby.ViewModels;

public class ServerViewModel : ViewModelBase
{
    private readonly Server _server;

    public ServerViewModel(Server server)
    {
        _server = server;
    }

    public Server Server => _server;
    public string ServerName => _server.ServerName;
    public string ServerUrl => _server.ServerUrl;
    public string Username => _server.Username;
    public string Password => _server.Password;
    public string AccessToken => _server.AccessToken;

}