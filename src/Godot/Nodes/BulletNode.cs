using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

public partial class BulletNode : Node2D
{
    private static readonly Color BulletColor = new(1f, 0.8f, 0.1f);

    public int BulletId { get; private set; }

    public void Initialize(int bulletId)
    {
        BulletId = bulletId;
    }

    public void UpdateFrom(BulletSnapshot snapshot)
    {
        Position = new Vector2(snapshot.X, snapshot.Y);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Constants.BulletRadius, BulletColor);
    }
}
