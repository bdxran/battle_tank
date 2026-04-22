using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Settings;

public class UpdateChecker
{
    private const string ApiUrl = "https://api.github.com/repos/randy/battle_tank/releases/latest";
    private const string Repo = "randy/battle_tank";

    public event Action<string, string, string>? UpdateAvailable; // (version, htmlUrl, assetUrl)

    public async Task CheckAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            client.DefaultRequestHeaders.Add("User-Agent", $"BattleTank/{Constants.GameVersion}");

            var json = await client.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = root.GetProperty("html_url").GetString() ?? "";
            var remoteVersion = tagName.TrimStart('v');

            if (!IsNewer(remoteVersion, Constants.GameVersion))
                return;

            var assetUrl = FindAssetUrl(root, remoteVersion);
            UpdateAvailable?.Invoke(remoteVersion, htmlUrl, assetUrl);
        }
        catch
        {
            // Silencieux — pas de réseau ou GitHub indisponible
        }
    }

    private static bool IsNewer(string remote, string local)
    {
        return Version.TryParse(remote, out var r)
            && Version.TryParse(local, out var l)
            && r > l;
    }

    private static string FindAssetUrl(JsonElement root, string version)
    {
        if (!root.TryGetProperty("assets", out var assets))
            return $"https://github.com/{Repo}/releases/download/v{version}/BattleTank-Setup.exe";

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase))
                return asset.GetProperty("browser_download_url").GetString() ?? "";
        }

        return $"https://github.com/{Repo}/releases/download/v{version}/BattleTank-Setup.exe";
    }
}
