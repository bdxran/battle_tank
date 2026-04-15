using System;
using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// Pause menu overlay shown when the player presses Escape during a game.
/// In solo/local mode the game loop is paused while this menu is visible.
/// In multiplayer mode the game continues on the server side.
/// </summary>
public partial class PauseMenuNode : CanvasLayer
{
    public event Action? ResumeRequested;
    public event Action? QuitRequested;

    public override void _Ready()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(220, 0);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        vbox.AddChild(margin);

        var inner = new VBoxContainer();
        inner.AddThemeConstantOverride("separation", 8);
        margin.AddChild(inner);

        var label = new Label { Text = "— Pause —" };
        label.HorizontalAlignment = HorizontalAlignment.Center;
        inner.AddChild(label);

        var resumeButton = new Button { Text = "Reprendre" };
        resumeButton.Pressed += () => ResumeRequested?.Invoke();
        inner.AddChild(resumeButton);

        var quitButton = new Button { Text = "Quitter" };
        quitButton.Pressed += () => QuitRequested?.Invoke();
        inner.AddChild(quitButton);

        Hide();
    }
}
