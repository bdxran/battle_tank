using Godot;

namespace BattleTank.Godot.UI;

public partial class KillFeedNode : CanvasLayer
{
    private const float EntryLifetime = 4f;
    private const int MaxEntries = 5;

    private VBoxContainer _container = null!;

    public override void _Ready()
    {
        _container = new VBoxContainer();
        _container.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _container.GrowHorizontal = Control.GrowDirection.Begin;
        _container.Position = new Vector2(-16, 16);
        AddChild(_container);
    }

    public void AddEntry(int killedId, int killerId)
    {
        // Trim oldest entries if over limit
        while (_container.GetChildCount() >= MaxEntries)
            _container.GetChild(0).QueueFree();

        string text = killerId > 0
            ? $"Player {killerId} eliminated Player {killedId}"
            : $"Player {killedId} was eliminated";

        var label = new Label
        {
            Text = text,
            Modulate = new Color(1f, 0.85f, 0.3f),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        _container.AddChild(label);

        var timer = new Timer
        {
            WaitTime = EntryLifetime,
            OneShot = true,
            Autostart = true,
        };
        label.AddChild(timer);
        timer.Timeout += label.QueueFree;
    }
}
