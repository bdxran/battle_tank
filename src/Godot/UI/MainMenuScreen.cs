using System;
using Godot;
using BattleTank.Godot.Settings;

namespace BattleTank.Godot.UI;

/// <summary>
/// First screen shown on launch. Replaces the automatic server connection.
/// </summary>
public partial class MainMenuScreen : CanvasLayer
{
    public event Action? SoloRequested;
    public event Action? HostRequested;
    public event Action? ConfigureServerRequested;
    public event Action? JoinRequested;
    public event Action? SettingsRequested;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(320, 200);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        var title = new Label { Text = "Battle Tank" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var solo = new Button { Text = "Jouer solo" };
        solo.Pressed += () => SoloRequested?.Invoke();
        vbox.AddChild(solo);

        var host = new Button { Text = "Héberger une partie" };
        host.Pressed += () => HostRequested?.Invoke();
        vbox.AddChild(host);

        var configServer = new Button { Text = "Configurer serveur dédié" };
        configServer.Pressed += () => ConfigureServerRequested?.Invoke();
        vbox.AddChild(configServer);

        var join = new Button { Text = "Rejoindre une partie" };
        join.Pressed += () => JoinRequested?.Invoke();
        vbox.AddChild(join);

        var settings = new Button { Text = "Paramètres" };
        settings.Pressed += () => SettingsRequested?.Invoke();
        vbox.AddChild(settings);

        var quit = new Button { Text = "Quitter" };
        quit.Pressed += () => GetTree().Quit();
        vbox.AddChild(quit);

        CheckForUpdateAsync();
    }

    private async void CheckForUpdateAsync()
    {
        var banner = new UpdateBannerNode();
        AddChild(banner);

        var checker = new UpdateChecker();
        checker.UpdateAvailable += (version, htmlUrl, assetUrl) =>
            banner.Show(version, htmlUrl, assetUrl);

        await checker.CheckAsync();
    }
}
