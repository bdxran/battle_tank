using System;
using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// Full-screen countdown overlay: 3 → 2 → 1 → GO!, then fires CountdownFinished.
/// </summary>
public partial class CountdownNode : CanvasLayer
{
    public event Action? CountdownFinished;

    private Label _label = null!;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        _label = new Label();
        _label.AddThemeFontSizeOverride("font_size", 96);
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        center.AddChild(_label);

        Hide();
    }

    public void StartCountdown()
    {
        Show();
        RunStep(3);
    }

    private void RunStep(int count)
    {
        if (count > 0)
        {
            _label.Text = count.ToString();
            var timer = GetTree().CreateTimer(1.0);
            timer.Timeout += () => RunStep(count - 1);
        }
        else
        {
            _label.Text = "GO!";
            var timer = GetTree().CreateTimer(0.8);
            timer.Timeout += () =>
            {
                Hide();
                CountdownFinished?.Invoke();
            };
        }
    }
}
