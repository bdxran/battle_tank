using System;
using System.Collections.Generic;
using System.Numerics;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;

namespace BattleTank.GameLogic.AI;

/// <summary>
/// Simple bot: roams randomly and rotates toward the nearest enemy to shoot it.
/// Changes movement direction every ~2 seconds.
/// </summary>
public sealed class SimpleBot : IBot
{
    private const uint MovementChangeTicks = 40; // 2s at 20 TPS
    private const float AimToleranceDegrees = 15f;

    private readonly Random _rng;
    private InputFlags _movementFlags;
    private uint _nextMovementChangeTick;

    public int PlayerId { get; }

    public SimpleBot(int playerId, Random? rng = null)
    {
        PlayerId = playerId;
        _rng = rng ?? new Random(playerId);
        _movementFlags = PickRandomMovement();
    }

    public InputFlags ComputeInput(IReadOnlyDictionary<int, TankEntity> tanks, uint currentTick)
    {
        if (!tanks.TryGetValue(PlayerId, out var self) || !self.IsAlive)
            return InputFlags.None;

        // Refresh random movement direction periodically
        if (currentTick >= _nextMovementChangeTick)
        {
            _movementFlags = PickRandomMovement();
            _nextMovementChangeTick = currentTick + MovementChangeTicks;
        }

        var target = FindNearestEnemy(self, tanks);
        if (target == null)
            return _movementFlags;

        float angleToTarget = AngleTo(self.Position, target.Position);
        float delta = NormalizeAngleDelta(angleToTarget - self.Rotation);

        InputFlags flags = _movementFlags;

        // Rotate toward target
        if (Math.Abs(delta) > AimToleranceDegrees)
        {
            flags = delta < 0
                ? (flags | InputFlags.RotateLeft)
                : (flags | InputFlags.RotateRight);
        }
        else
        {
            // Aimed well enough: fire
            flags |= InputFlags.Fire;
        }

        return flags;
    }

    private InputFlags PickRandomMovement()
    {
        return _rng.Next(4) switch
        {
            0 => InputFlags.MoveForward,
            1 => InputFlags.MoveBackward,
            2 => InputFlags.MoveForward | InputFlags.RotateLeft,
            _ => InputFlags.MoveForward | InputFlags.RotateRight,
        };
    }

    private static TankEntity? FindNearestEnemy(TankEntity self, IReadOnlyDictionary<int, TankEntity> tanks)
    {
        TankEntity? nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var (id, tank) in tanks)
        {
            if (id == self.Id || !tank.IsAlive) continue;

            float dx = tank.Position.X - self.Position.X;
            float dy = tank.Position.Y - self.Position.Y;
            float dist = dx * dx + dy * dy;

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = tank;
            }
        }

        return nearest;
    }

    /// <summary>Returns the angle in degrees pointing from <paramref name="from"/> to <paramref name="to"/>, 0 = up.</summary>
    private static float AngleTo(Vector2 from, Vector2 to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        // atan2 returns angle from positive X axis; convert to game convention (0 = up, CW positive)
        float radians = MathF.Atan2(dx, -dy);
        float degrees = radians * 180f / MathF.PI;
        return NormalizeAngle(degrees);
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f) degrees += 360f;
        return degrees;
    }

    /// <summary>Returns delta in [-180, 180].</summary>
    private static float NormalizeAngleDelta(float delta)
    {
        delta %= 360f;
        if (delta > 180f) delta -= 360f;
        if (delta < -180f) delta += 360f;
        return delta;
    }
}
