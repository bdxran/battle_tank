using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

/// <summary>
/// Minimap overlay. Draws a scaled-down view of the map with tank positions and safe zone.
/// Add as child of a CanvasLayer to keep it in screen space.
/// </summary>
public partial class MinimapNode : Control
{
    private const float MapSize = 120f;   // minimap square size in pixels
    private const float Margin = 12f;     // distance from bottom-right corner
    private const float TankDotRadius = 3f;
    private const float BorderWidth = 1f;

    private static readonly Color BackgroundColor = new(0f, 0f, 0f, 0.55f);
    private static readonly Color BorderColor = new(1f, 1f, 1f, 0.4f);
    private static readonly Color LocalTankColor = new(0.3f, 0.6f, 1f);
    private static readonly Color EnemyTankColor = new(0.9f, 0.3f, 0.3f);
    private static readonly Color DeadTankColor = new(0.4f, 0.4f, 0.4f, 0.5f);
    private static readonly Color ZoneBorderColor = new(0.3f, 1f, 0.4f, 0.7f);

    private TankSnapshot[] _tanks = [];
    private ZoneSnapshot _zone = new(500f, 500f, Constants.ZoneInitialRadius, Constants.ZoneDamagePerSecond);
    private int _localPlayerId;

    public void Initialize(int localPlayerId)
    {
        _localPlayerId = localPlayerId;
        SetAnchorsPreset(LayoutPreset.BottomRight);
        // Position so the minimap sits in the bottom-right with margin
        OffsetRight = -Margin;
        OffsetBottom = -Margin;
        OffsetLeft = OffsetRight - MapSize;
        OffsetTop = OffsetBottom - MapSize;
        Size = new Vector2(MapSize, MapSize);
    }

    public void UpdateFrom(TankSnapshot[] tanks, ZoneSnapshot zone)
    {
        _tanks = tanks;
        _zone = zone;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Background
        DrawRect(new Rect2(Vector2.Zero, Size), BackgroundColor);
        DrawRect(new Rect2(Vector2.Zero, Size), BorderColor, false, BorderWidth);

        // Safe zone circle
        var center = WorldToMinimap(_zone.CenterX, _zone.CenterY);
        float radius = _zone.Radius / Constants.MapWidth * MapSize;
        DrawArc(center, radius, 0f, Mathf.Tau, 48, ZoneBorderColor, 1.5f);

        // Tanks
        foreach (var tank in _tanks)
        {
            var pos = WorldToMinimap(tank.X, tank.Y);
            var color = tank.Health <= 0
                ? DeadTankColor
                : tank.Id == _localPlayerId ? LocalTankColor : EnemyTankColor;
            DrawCircle(pos, TankDotRadius, color);
        }
    }

    private Vector2 WorldToMinimap(float worldX, float worldY) =>
        new(worldX / Constants.MapWidth * MapSize, worldY / Constants.MapHeight * MapSize);
}
