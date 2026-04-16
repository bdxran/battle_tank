using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

public partial class TankNode : Node2D
{
    private static readonly Color LocalBodyColor = new(0.2f, 0.4f, 0.8f);
    private static readonly Color AllyBodyColor = new(0.2f, 0.7f, 0.2f);
    private static readonly Color EnemyBodyColor = new(0.8f, 0.2f, 0.2f);
    private static readonly Color BarrelColor = new(0.1f, 0.4f, 0.1f);
    private static readonly Color DeadColor = new(0.4f, 0.4f, 0.4f);
    private static readonly Color FlashColor = new(1f, 1f, 1f, 0.6f);

    private const float FlashDuration = 0.15f;
    private const float ExplosionDuration = 0.5f;

    private bool _isLocal;
    private bool _isAlly;
    private bool _isAlive = true;
    private int _prevHealth = Constants.TankMaxHealth;
    private float _flashTimer;
    private float _explosionTimer;

    public int PlayerId { get; private set; }

    public void Initialize(int playerId, bool isLocal, bool isAlly = false)
    {
        PlayerId = playerId;
        _isLocal = isLocal;
        _isAlly = isAlly;
    }

    public void UpdateFrom(TankSnapshot snapshot)
    {
        bool wasAlive = _isAlive;
        int newHealth = snapshot.Health;

        if (newHealth < _prevHealth && wasAlive && newHealth > 0)
            _flashTimer = FlashDuration;

        if (newHealth <= 0 && wasAlive)
            _explosionTimer = ExplosionDuration;

        _prevHealth = newHealth;
        _isAlive = newHealth > 0;
        Position = new Vector2(snapshot.X, snapshot.Y);
        RotationDegrees = snapshot.Rotation;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_flashTimer > 0)
        {
            _flashTimer -= (float)delta;
            QueueRedraw();
        }

        if (_explosionTimer > 0)
        {
            _explosionTimer -= (float)delta;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_explosionTimer > 0)
        {
            DrawExplosion();
            return;
        }

        if (!_isAlive)
        {
            DrawCircle(Vector2.Zero, Constants.TankRadius, DeadColor);
            return;
        }

        var bodyColor = _isLocal ? LocalBodyColor : (_isAlly ? AllyBodyColor : EnemyBodyColor);

        // Tank body (square)
        DrawRect(new Rect2(-Constants.TankRadius, -Constants.TankRadius,
            Constants.TankRadius * 2, Constants.TankRadius * 2), bodyColor);

        // Barrel (pointing up / forward)
        DrawRect(new Rect2(-4, -Constants.TankRadius - 12, 8, 14), BarrelColor);

        // Damage flash overlay
        if (_flashTimer > 0)
        {
            DrawRect(new Rect2(-Constants.TankRadius, -Constants.TankRadius,
                Constants.TankRadius * 2, Constants.TankRadius * 2), FlashColor);
        }
    }

    private void DrawExplosion()
    {
        float progress = 1f - (_explosionTimer / ExplosionDuration); // 0..1
        float maxRadius = Constants.TankRadius * 3f;

        for (int i = 0; i < 3; i++)
        {
            float ringProgress = Mathf.Clamp(progress * 1.5f - i * 0.15f, 0f, 1f);
            float radius = maxRadius * ringProgress;
            float alpha = 1f - ringProgress;
            var color = i == 0
                ? new Color(1f, 0.6f, 0.1f, alpha)
                : i == 1
                    ? new Color(1f, 0.2f, 0.05f, alpha * 0.7f)
                    : new Color(0.3f, 0.3f, 0.3f, alpha * 0.5f); // smoke
            DrawCircle(Vector2.Zero, radius, color);
        }
    }
}
