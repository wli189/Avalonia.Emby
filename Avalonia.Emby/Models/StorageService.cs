using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Avalonia.Emby.Models;

public class StorageService
{
    private readonly string _storageFile;

    public StorageService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Avalonia.Emby"
        );

        // Create directory if it doesn't exist
        Directory.CreateDirectory(appDataPath);

        _storageFile = Path.Combine(appDataPath, "servers.json");
    }

    public async Task SaveServersAsync(IEnumerable<Account> servers)
    {
        var json = JsonSerializer.Serialize(servers, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_storageFile, json);
    }

    public async Task<List<Account>> LoadServersAsync()
    {
        if (!File.Exists(_storageFile))
        {
            return new List<Account>();
        }

        var json = await File.ReadAllTextAsync(_storageFile);
        return JsonSerializer.Deserialize<List<Account>>(json) ?? new List<Account>();
    }
}