using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace BattleTank.Godot.Persistence;

public record UserPreferences(string LastUsername);

public class UserPreferencesRepository
{
    private readonly string _path;

    public UserPreferencesRepository()
    {
        _path = Path.Combine(OS.GetUserDataDir(), "preferences.json");
    }

    public UserPreferences Load()
    {
        try
        {
            if (!File.Exists(_path)) return new UserPreferences("");
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences("");
        }
        catch (Exception)
        {
            return new UserPreferences("");
        }
    }

    public void Save(UserPreferences prefs)
    {
        var json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
