namespace Avalonia.Emby.ViewModels;

public class ServerViewModel : ViewModelBase
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Username { get; set; }
    public string AccessToken { get; set; }

    public ServerViewModel(string name, string url, string username, string accessToken)
    {
        Name = name;
        Url = url;
        Username = username;
        AccessToken = accessToken;
    }
}