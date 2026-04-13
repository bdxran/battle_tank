using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

public partial class ZoneNode : Node2D
{
    private static readonly Color SafeZoneColor = new(0.2f, 0.9f, 0.2f, 0.08f);
    private static readonly Color BorderColor = new(1f, 1f, 1f, 0.6f);

    private float _radius = Constants.ZoneInitialRadius;
    private Vector2 _center = new(Constants.MapWidth / 2f, Constants.MapHeight / 2f);

    public void UpdateFrom(ZoneSnapshot snapshot)
    {
        _center = new Vector2(snapshot.CenterX, snapshot.CenterY);
        _radius = snapshot.Radius;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(_center, _radius, SafeZoneColor);
        DrawArc(_center, _radius, 0f, Mathf.Tau, 64, BorderColor, 2f);
    }
}
