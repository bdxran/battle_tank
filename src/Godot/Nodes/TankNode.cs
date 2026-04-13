using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

public partial class TankNode : Node2D
{
    private static readonly Color BodyColor = new(0.2f, 0.6f, 0.2f);
    private static readonly Color LocalBodyColor = new(0.2f, 0.4f, 0.8f);
    private static readonly Color BarrelColor = new(0.1f, 0.4f, 0.1f);
    private static readonly Color DeadColor = new(0.4f, 0.4f, 0.4f);

    private bool _isLocal;
    private bool _isAlive = true;

    public int PlayerId { get; private set; }

    public void Initialize(int playerId, bool isLocal)
    {
        PlayerId = playerId;
        _isLocal = isLocal;
    }

    public void UpdateFrom(TankSnapshot snapshot)
    {
        Position = new Vector2(snapshot.X, snapshot.Y);
        RotationDegrees = snapshot.Rotation;
        _isAlive = snapshot.Health > 0;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_isAlive)
        {
            DrawCircle(Vector2.Zero, Constants.TankRadius, DeadColor);
            return;
        }

        var bodyColor = _isLocal ? LocalBodyColor : BodyColor;

        // Tank body (square)
        DrawRect(new Rect2(-Constants.TankRadius, -Constants.TankRadius,
            Constants.TankRadius * 2, Constants.TankRadius * 2), bodyColor);

        // Barrel (pointing up / forward)
        DrawRect(new Rect2(-4, -Constants.TankRadius - 12, 8, 14), BarrelColor);
    }
}
