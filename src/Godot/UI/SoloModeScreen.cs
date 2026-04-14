using System;
using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

/// <summary>
/// Mode selector for offline solo play. Shows all game modes with bot fill.
/// </summary>
public partial class SoloModeScreen : CanvasLayer
{
    public event Action<GameMode, string>? SoloModeSelected;
    public event Action? BackRequested;

    private LineEdit _nicknameField = null!;

    public override void _Ready()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(320, 320);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        var title = new Label { Text = "Jouer solo — choisir un mode" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _nicknameField = new LineEdit { PlaceholderText = "Pseudo", Text = "Joueur1" };
        vbox.AddChild(_nicknameField);

        AddModeButton(vbox, "Entraînement", GameMode.Training);
        AddModeButton(vbox, "Battle Royale", GameMode.BattleRoyale);
        AddModeButton(vbox, "Teams (2v2/4v4)", GameMode.Teams);
        AddModeButton(vbox, "Deathmatch", GameMode.Deathmatch);
        AddModeButton(vbox, "Capture de zone", GameMode.CaptureZone);

        var back = new Button { Text = "← Retour" };
        back.Pressed += () => BackRequested?.Invoke();
        vbox.AddChild(back);
    }

    private void AddModeButton(VBoxContainer parent, string label, GameMode mode)
    {
        var btn = new Button { Text = label };
        btn.Pressed += () =>
        {
            string nick = _nicknameField.Text.Trim();
            if (string.IsNullOrEmpty(nick)) nick = "Joueur1";
            SoloModeSelected?.Invoke(mode, nick);
        };
        parent.AddChild(btn);
    }
}
