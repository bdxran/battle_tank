using System;
using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// First screen shown on launch. Replaces the automatic server connection.
/// </summary>
public partial class MainMenuScreen : CanvasLayer
{
    public event Action? SoloRequested;
    public event Action? HostRequested;
    public event Action? JoinRequested;

    public override void _Ready()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(320, 200);
        AddChild(panel);

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

        var join = new Button { Text = "Rejoindre une partie" };
        join.Pressed += () => JoinRequested?.Invoke();
        vbox.AddChild(join);
    }
}
