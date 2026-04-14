using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

public partial class BulletNode : Node2D
{
    private static readonly Color BulletColor = new(1f, 0.8f, 0.1f);
    private static readonly Color FlashColor = new(1f, 0.5f, 0.05f);

    private const float FlashDuration = 0.12f;
    private const float FlashMaxRadius = 10f;

    private float _flashTimer;

    public int BulletId { get; private set; }

    public void Initialize(int bulletId)
    {
        BulletId = bulletId;
        _flashTimer = FlashDuration;
    }

    public void UpdateFrom(BulletSnapshot snapshot)
    {
        Position = new Vector2(snapshot.X, snapshot.Y);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_flashTimer > 0)
        {
            _flashTimer -= (float)delta;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_flashTimer > 0)
        {
            float progress = _flashTimer / FlashDuration;
            float radius = FlashMaxRadius * progress;
            var color = new Color(FlashColor.R, FlashColor.G, FlashColor.B, progress);
            DrawCircle(Vector2.Zero, radius, color);
        }

        DrawCircle(Vector2.Zero, Constants.BulletRadius, BulletColor);
    }
}
