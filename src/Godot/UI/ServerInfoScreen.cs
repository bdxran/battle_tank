using System;
using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// Shown after a server starts. Displays connection info to share with other players.
/// </summary>
public partial class ServerInfoScreen : CanvasLayer
{
    public event Action? PlayRequested;
    public event Action? StopRequested;

    private Label _infoLabel = null!;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(360, 220);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        var title = new Label { Text = "Serveur démarré" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _infoLabel = new Label { Text = "Chargement…" };
        _infoLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        _infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_infoLabel);

        var hint = new Label { Text = "Partagez ces informations avec vos amis." };
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(hint);

        var play = new Button { Text = "Commencer à jouer" };
        play.Pressed += () => PlayRequested?.Invoke();
        vbox.AddChild(play);

        var stop = new Button { Text = "Arrêter le serveur" };
        stop.Pressed += () => StopRequested?.Invoke();
        vbox.AddChild(stop);
    }

    public void SetInfo(string localIp, int port, string? roomCode)
    {
        string codeText = string.IsNullOrEmpty(roomCode) ? "Aucun" : roomCode;
        _infoLabel.Text = $"IP : {localIp}\nPort : {port}\nCode : {codeText}";
    }
}
