using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// Overlay displayed when the local player is eliminated but the game is still in progress.
/// Shows a "SPECTATING" banner and the current alive count.
/// </summary>
public partial class SpectatorOverlayNode : CanvasLayer
{
    private Label _spectatingLabel = null!;
    private Label _aliveLabel = null!;

    public override void _Ready()
    {
        _spectatingLabel = new Label
        {
            Text = "SPECTATING",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _spectatingLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        _spectatingLabel.Position = new Vector2(0, 24);
        AddChild(_spectatingLabel);

        _aliveLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _aliveLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        _aliveLabel.Position = new Vector2(0, 52);
        AddChild(_aliveLabel);

        Hide();
    }

    public void UpdateAliveCount(int count)
    {
        _aliveLabel.Text = $"{count} player{(count == 1 ? "" : "s")} remaining";
    }
}
