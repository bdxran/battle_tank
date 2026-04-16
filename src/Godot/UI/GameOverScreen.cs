using System;
using Godot;

namespace BattleTank.Godot.UI;

public partial class GameOverScreen : CanvasLayer
{
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Button _restartButton = null!;
    private Button _menuButton = null!;

    public event Action? RestartRequested;
    public event Action? MenuRequested;

    public override void _Ready()
    {
        Visible = false;

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        center.AddChild(vbox);

        _titleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "GAME OVER"
        };

        _subtitleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = ""
        };

        var btnContainer = new HBoxContainer();
        btnContainer.AddThemeConstantOverride("separation", 12);
        btnContainer.Alignment = BoxContainer.AlignmentMode.Center;

        _restartButton = new Button { Text = "Rejouer" };
        _restartButton.Pressed += () => RestartRequested?.Invoke();

        _menuButton = new Button { Text = "Menu principal" };
        _menuButton.Pressed += () => MenuRequested?.Invoke();

        btnContainer.AddChild(_restartButton);
        btnContainer.AddChild(_menuButton);

        vbox.AddChild(_titleLabel);
        vbox.AddChild(_subtitleLabel);
        vbox.AddChild(btnContainer);
    }

    public void ShowWin(int localPlayerId, int winnerId)
    {
        _titleLabel.Text = winnerId == localPlayerId ? "VICTOIRE !" : "DÉFAITE";
        _subtitleLabel.Text = winnerId == -1
            ? "Aucun survivant."
            : winnerId == localPlayerId
                ? "Vous êtes le dernier tank en vie."
                : $"Joueur {winnerId} remporte la partie.";
        Visible = true;
    }

    public void ShowEliminated(int killerId)
    {
        _titleLabel.Text = "ÉLIMINÉ";
        _subtitleLabel.Text = killerId == -1
            ? "Éliminé par la zone."
            : $"Éliminé par le Joueur {killerId}.";
        Visible = true;
    }
}
