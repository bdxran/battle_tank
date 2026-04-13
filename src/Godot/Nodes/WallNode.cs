using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

public partial class WallNode : Node2D
{
    private static readonly Color WallColor = new(0.45f, 0.35f, 0.25f);
    private static readonly Color WallBorder = new(0.3f, 0.22f, 0.15f);

    private WallData _wall = null!;

    public void Initialize(WallData wall)
    {
        _wall = wall;
    }

    public override void _Draw()
    {
        var rect = new Rect2(_wall.X, _wall.Y, _wall.Width, _wall.Height);
        DrawRect(rect, WallColor);
        DrawRect(rect, WallBorder, false, 1.5f);
    }
}
