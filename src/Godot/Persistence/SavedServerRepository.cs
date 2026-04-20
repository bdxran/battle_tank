using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace BattleTank.Godot.Persistence;

public record SavedServer(string Name, string Address, int Port);

public class SavedServerRepository
{
    private readonly string _path;

    public SavedServerRepository()
    {
        _path = System.IO.Path.Combine(OS.GetUserDataDir(), "servers.json");
    }

    public List<SavedServer> Load()
    {
        try
        {
            if (!System.IO.File.Exists(_path)) return [];
            var json = System.IO.File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<SavedServer>>(json) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public void Save(List<SavedServer> servers)
    {
        var json = JsonSerializer.Serialize(servers, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_path, json);
    }
}
