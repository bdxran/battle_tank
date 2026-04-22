using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Godot;
using BattleTank.GameLogic.Shared;
using HttpClient = System.Net.Http.HttpClient;

namespace BattleTank.Godot.Settings;

public static class UpdateLauncher
{
    public static async Task StartUpdateAsync(string version, string setupExeUrl)
    {
        if (OS.GetName() == "Windows")
            await UpdateWindowsAsync(version, setupExeUrl);
        else
            UpdateLinux();
    }

    private static async Task UpdateWindowsAsync(string version, string setupExeUrl)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"BattleTank-Setup-{version}.exe");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", $"BattleTank/{Constants.GameVersion}");
        var bytes = await client.GetByteArrayAsync(setupExeUrl);
        await File.WriteAllBytesAsync(tempPath, bytes);

        Process.Start(new ProcessStartInfo
        {
            FileName = tempPath,
            Arguments = "/VERYSILENT /NORESTART /RESTARTAPPLICATIONS",
            UseShellExecute = true,
        });

        (Engine.GetMainLoop() as SceneTree)?.Quit();
    }

    private static void UpdateLinux()
    {
        var updateScript = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "..", "share", "games", "battle-tank", "update.sh");

        if (!File.Exists(updateScript))
        {
            GD.PrintErr("[UpdateLauncher] update.sh introuvable — réinstaller manuellement.");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"\"{updateScript}\"",
            UseShellExecute = true,
        });

        (Engine.GetMainLoop() as SceneTree)?.Quit();
    }
}
