using Godot;

namespace BattleTank.Godot.UI;

public partial class HudNode : CanvasLayer
{
    private Label _healthLabel = null!;
    private Label _aliveLabel = null!;

    public override void _Ready()
    {
        var container = new VBoxContainer();
        container.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        container.Position = new Vector2(16, 16);
        AddChild(container);

        _healthLabel = new Label { Text = "HP: 100" };
        _aliveLabel = new Label { Text = "Alive: -" };

        container.AddChild(_healthLabel);
        container.AddChild(_aliveLabel);
    }

    public void UpdateHealth(int health)
    {
        _healthLabel.Text = $"HP: {health}";
    }

    public void UpdateAliveCount(int count)
    {
        _aliveLabel.Text = $"Alive: {count}";
    }
}
