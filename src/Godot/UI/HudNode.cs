using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

public partial class HudNode : CanvasLayer
{
    private Label _healthLabel = null!;
    private Label _aliveLabel = null!;
    private MinimapNode _minimap = null!;

    public void Initialize(int localPlayerId)
    {
        _minimap.Initialize(localPlayerId);
    }

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

        _minimap = new MinimapNode();
        AddChild(_minimap);
    }

    public void UpdateHealth(int health)
    {
        _healthLabel.Text = $"HP: {health}";
    }

    public void UpdateAliveCount(int count)
    {
        _aliveLabel.Text = $"Alive: {count}";
    }

    public void UpdateMinimap(TankSnapshot[] tanks, ZoneSnapshot zone)
    {
        _minimap.UpdateFrom(tanks, zone);
    }
}
