namespace Avalonia.Emby.Models;

public class Server
{
    public string ServerName { get; set; }
    public string ServerUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string AccessToken { get; set; }

    public Server(string serverName, string serverUrl, string username, string password, string accessToken)
    {
        ServerName = serverName;
        ServerUrl = serverUrl;
        Username = username;
        Password = password;
        AccessToken = accessToken;
    }
}