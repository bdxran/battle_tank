using Godot;

namespace BattleTank.Godot.UI;

public partial class GameOverScreen : CanvasLayer
{
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Label _hintLabel = null!;

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

        _hintLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "Disconnecting..."
        };

        vbox.AddChild(_titleLabel);
        vbox.AddChild(_subtitleLabel);
        vbox.AddChild(_hintLabel);
    }

    public void ShowWin(int localPlayerId, int winnerId)
    {
        _titleLabel.Text = winnerId == localPlayerId ? "VICTORY!" : "DEFEAT";
        _subtitleLabel.Text = winnerId == -1
            ? "No survivors."
            : winnerId == localPlayerId
                ? "You are the last tank standing."
                : $"Player {winnerId} wins.";
        Visible = true;
    }

    public void ShowEliminated(int killerId)
    {
        _titleLabel.Text = "ELIMINATED";
        _subtitleLabel.Text = killerId == -1
            ? "Killed by the zone."
            : $"Killed by Player {killerId}.";
        Visible = true;
    }
}
