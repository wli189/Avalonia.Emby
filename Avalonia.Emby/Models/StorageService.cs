using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Avalonia.Emby.Models;

public class Config
{
    public string DeviceId { get; set; }
}

public class StorageService
{
    private readonly string _accountFile;
    private readonly string _configFile;

    public StorageService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Avalonia.Emby"
        );

        // Create directory if it doesn't exist
        Directory.CreateDirectory(appDataPath);

        _accountFile = Path.Combine(appDataPath, "accounts.json");
        _configFile = Path.Combine(appDataPath, "config.json");
    }

    public string GetDeviceId()
    {
        Config config;

        if (File.Exists(_configFile))
        {
            var json = File.ReadAllText(_configFile);
            config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
        }
        else
        {
            config = new Config();
        }

        if (string.IsNullOrEmpty(config.DeviceId))
        {
            config.DeviceId = Guid.NewGuid().ToString(); // Generate a new deviceId
            File.WriteAllText(_configFile, JsonSerializer.Serialize(config)); // Save it
        }

        return config.DeviceId;
    }

    public async Task SaveAccountsAsync(IEnumerable<Account> accounts)
    {
        var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_accountFile, json);
    }

    public async Task<List<Account>> LoadAccountsAsync()
    {
        if (!File.Exists(_accountFile))
        {
            return new List<Account>();
        }

        var json = await File.ReadAllTextAsync(_accountFile);
        return JsonSerializer.Deserialize<List<Account>>(json) ?? new List<Account>();
    }
}