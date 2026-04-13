using System;
using System.Numerics;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Entities;

public class TankEntity
{
    private int _health;

    public int Id { get; }
    public Vector2 Position { get; private set; }
    public float Rotation { get; private set; } // degrees, 0 = facing up
    public int Health => _health;
    public bool IsAlive => _health > 0;

    public TankEntity(int id, Vector2 position)
    {
        Id = id;
        Position = position;
        _health = Constants.TankMaxHealth;
        Rotation = 0f;
    }

    public void ApplyInput(InputFlags flags, float deltaTime)
    {
        if (!IsAlive) return;

        if ((flags & InputFlags.RotateLeft) != 0)
            Rotation -= Constants.TankRotationSpeed * deltaTime;

        if ((flags & InputFlags.RotateRight) != 0)
            Rotation += Constants.TankRotationSpeed * deltaTime;

        Rotation = NormalizeAngle(Rotation);

        if ((flags & InputFlags.MoveForward) != 0)
            Position += GetForwardVector() * Constants.TankMoveSpeed * deltaTime;

        if ((flags & InputFlags.MoveBackward) != 0)
            Position -= GetForwardVector() * Constants.TankMoveSpeed * deltaTime;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        _health = Math.Max(0, _health - amount);
    }

    public TankSnapshot GetSnapshot() =>
        new(Id, Position.X, Position.Y, Rotation, _health);

    private Vector2 GetForwardVector()
    {
        float radians = Rotation * MathF.PI / 180f;
        return new Vector2(MathF.Sin(radians), -MathF.Cos(radians));
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f) degrees += 360f;
        return degrees;
    }
}
