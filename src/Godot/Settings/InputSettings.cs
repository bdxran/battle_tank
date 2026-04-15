using Godot;

namespace BattleTank.Godot.Settings;

/// <summary>
/// Loads and saves key bindings from/to user://settings.cfg.
/// Call Load() once at startup to register all input actions in InputMap.
/// </summary>
public static class InputSettings
{
    private const string ConfigPath = "user://settings.cfg";
    private const string Section = "keybindings";

    public static readonly (string Action, string Label, Key DefaultKey)[] Actions =
    [
        ("move_forward",  "Avancer",        Key.Z),
        ("move_backward", "Reculer",        Key.S),
        ("rotate_left",   "Tourner gauche", Key.Q),
        ("rotate_right",  "Tourner droite", Key.D),
        ("fire",          "Tirer",          Key.Space),
    ];

    public static void Load()
    {
        var cfg = new ConfigFile();
        var err = cfg.Load(ConfigPath);

        foreach (var (action, _, defaultKey) in Actions)
        {
            var key = err == Error.Ok
                ? (Key)(int)cfg.GetValue(Section, action, (int)defaultKey)
                : defaultKey;

            RegisterAction(action, key);
        }

        if (err != Error.Ok)
            Save();
    }

    public static void Save()
    {
        var cfg = new ConfigFile();

        foreach (var (action, _, _) in Actions)
        {
            var key = GetCurrentKey(action);
            cfg.SetValue(Section, action, (int)key);
        }

        cfg.Save(ConfigPath);
    }

    public static void Rebind(string action, Key newKey)
    {
        InputMap.ActionEraseEvents(action);
        var ev = new InputEventKey { Keycode = newKey };
        InputMap.ActionAddEvent(action, ev);
        Save();
    }

    public static Key GetCurrentKey(string action)
    {
        foreach (var ev in InputMap.ActionGetEvents(action))
        {
            if (ev is InputEventKey k)
                return k.Keycode;
        }
        return Key.None;
    }

    private static void RegisterAction(string action, Key key)
    {
        if (!InputMap.HasAction(action))
            InputMap.AddAction(action);
        else
            InputMap.ActionEraseEvents(action);

        var ev = new InputEventKey { Keycode = key };
        InputMap.ActionAddEvent(action, ev);
    }
}
