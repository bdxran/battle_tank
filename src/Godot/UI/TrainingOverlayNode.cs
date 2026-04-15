using System;
using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// Overlay displayed during a training session.
/// Provides a button to join a ranked game. Quitting is handled via the pause menu (Escape).
/// </summary>
public partial class TrainingOverlayNode : CanvasLayer
{
    public event Action? JoinRankedRequested;

    private Button _joinButton = null!;

    public override void _Ready()
    {
        var anchor = new Control();
        anchor.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        anchor.Position = new Vector2(10, 10);
        AddChild(anchor);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        anchor.AddChild(vbox);

        var label = new Label { Text = "— Mode Entraînement —" };
        label.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(label);

        _joinButton = new Button { Text = "Rejoindre une partie classée" };
        _joinButton.Pressed += () => JoinRankedRequested?.Invoke();
        vbox.AddChild(_joinButton);

        Hide();
    }

    public void SetLocalMode(bool isLocal)
    {
        _joinButton.Visible = !isLocal;
    }
}
