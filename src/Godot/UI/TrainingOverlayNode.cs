using System;
using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// Overlay displayed during a training session.
/// Provides buttons to join a ranked game or quit.
/// </summary>
public partial class TrainingOverlayNode : CanvasLayer
{
    public event Action? JoinRankedRequested;
    public event Action? QuitRequested;

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

        var joinButton = new Button { Text = "Rejoindre une partie" };
        joinButton.Pressed += () => JoinRankedRequested?.Invoke();
        vbox.AddChild(joinButton);

        var quitButton = new Button { Text = "Quitter" };
        quitButton.Pressed += () => QuitRequested?.Invoke();
        vbox.AddChild(quitButton);

        Hide();
    }
}
