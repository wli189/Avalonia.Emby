using System.Text.Json.Serialization;

namespace Avalonia.Emby.Models;

public class AuthResponse
{
    [JsonPropertyName("AccessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("ServerId")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("User")]
    public UserDto User { get; set; } = null!;

    [JsonPropertyName("SessionInfo")]
    public SessionInfo SessionInfo { get; set; } = null!;
}

public class SessionInfo
{
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("Client")]
    public string Client { get; set; } = string.Empty;

    [JsonPropertyName("DeviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("DeviceId")]
    public string DeviceId { get; set; } = string.Empty;
    
    [JsonPropertyName("ApplicationVersion")]
    public string ApplicationVersion { get; set; } = string.Empty;
}

public class ServerInfo
{
    [JsonPropertyName("ServerName")]
    public string ServerName { get; set; } = string.Empty;
}

public class UserDto
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
}