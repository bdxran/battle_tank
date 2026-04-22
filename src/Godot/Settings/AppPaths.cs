using System.IO;
using Godot;

namespace BattleTank.Godot.Settings;

public static class AppPaths
{
    private const string AppName = "BattleTank";

    public static string UserDataDir { get; } = Path.Combine(
        OS.GetSystemDir(OS.SystemDir.Documents), AppName);

    public static string DatabasePath => Path.Combine(UserDataDir, "battle_tank.db");
    public static string SettingsPath => Path.Combine(UserDataDir, "settings.cfg");
    public static string CrashReportsDir => Path.Combine(UserDataDir, "crash_reports");

    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(UserDataDir);
        Directory.CreateDirectory(CrashReportsDir);
    }
}
