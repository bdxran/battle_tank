using System;
using System.Collections.Generic;
using System.Numerics;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Physics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.AI;

/// <summary>
/// Simple bot: roams randomly, rotates toward the nearest enemy to shoot it.
/// In modes with control points, also moves toward uncaptured or enemy-held zones.
/// Changes movement direction every ~2 seconds.
/// </summary>
public sealed class SimpleBot : IBot
{
    private const uint MovementChangeTicks = 40; // 2s at 20 TPS
    private const float AimToleranceDegrees = 15f;
    private const int StuckThresholdTicks = 20;   // 1s at 20 TPS
    private const float StuckDistanceSq = 1f;     // 1px² — considered stuck below this

    private readonly Random _rng;
    private InputFlags _movementFlags;
    private uint _nextMovementChangeTick;
    private Vector2 _lastPosition;
    private int _stuckTicks;

    public int PlayerId { get; }

    public SimpleBot(int playerId, Random? rng = null)
    {
        PlayerId = playerId;
        _rng = rng ?? new Random(playerId);
        _movementFlags = PickRandomMovement();
    }

    public InputFlags ComputeInput(
        IReadOnlyDictionary<int, TankEntity> tanks,
        IReadOnlyList<ControlPoint> controlPoints,
        uint currentTick)
    {
        if (!tanks.TryGetValue(PlayerId, out var self) || !self.IsAlive)
            return InputFlags.None;

        // Detect wall-stuck: if the bot hasn't moved for StuckThresholdTicks, pick a new direction immediately
        float ddx = self.Position.X - _lastPosition.X;
        float ddy = self.Position.Y - _lastPosition.Y;
        if (ddx * ddx + ddy * ddy < StuckDistanceSq)
        {
            _stuckTicks++;
            if (_stuckTicks >= StuckThresholdTicks)
            {
                _movementFlags = PickRandomMovement();
                _nextMovementChangeTick = currentTick + MovementChangeTicks;
                _stuckTicks = 0;
            }
        }
        else
        {
            _stuckTicks = 0;
        }
        _lastPosition = self.Position;

        // Refresh random movement direction periodically
        if (currentTick >= _nextMovementChangeTick)
        {
            _movementFlags = PickRandomMovement();
            _nextMovementChangeTick = currentTick + MovementChangeTicks;
        }

        // Determine base movement: zone-directed when available, random otherwise
        ControlPoint? zone = FindValuableZone(self, controlPoints);
        InputFlags baseMovement;
        if (zone != null)
        {
            float angleToZone = AngleTo(self.Position, zone.Position);
            float zoneDelta = NormalizeAngleDelta(angleToZone - self.Rotation);
            baseMovement = InputFlags.MoveForward;
            if (Math.Abs(zoneDelta) > AimToleranceDegrees)
                baseMovement |= zoneDelta < 0 ? InputFlags.RotateLeft : InputFlags.RotateRight;
        }
        else
        {
            baseMovement = _movementFlags;
        }

        // Overlay enemy aiming on top of zone movement (rotation override, keep forward)
        var target = FindNearestEnemy(self, tanks);
        if (target != null)
        {
            float angleToTarget = AngleTo(self.Position, target.Position);
            float delta = NormalizeAngleDelta(angleToTarget - self.Rotation);

            InputFlags flags = baseMovement & ~InputFlags.RotateLeft & ~InputFlags.RotateRight;

            if (Math.Abs(delta) > AimToleranceDegrees)
                flags |= delta < 0 ? InputFlags.RotateLeft : InputFlags.RotateRight;
            else if (CollisionSystem.HasLineOfSight(self.Position, target.Position, MapLayout.Walls))
                flags |= InputFlags.Fire;

            return flags;
        }

        return baseMovement;
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
            if (self.TeamId >= 0 && tank.TeamId == self.TeamId) continue;

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

    private static ControlPoint? FindValuableZone(TankEntity self, IReadOnlyList<ControlPoint> controlPoints)
    {
        ControlPoint? nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < controlPoints.Count; i++)
        {
            ControlPoint cp = controlPoints[i];
            if (self.TeamId >= 0 && cp.ControllingTeamId == self.TeamId) continue;

            float dx = cp.Position.X - self.Position.X;
            float dy = cp.Position.Y - self.Position.Y;
            float dist = dx * dx + dy * dy;

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = cp;
            }
        }

        return nearest;
    }

    /// <summary>Returns the angle in degrees pointing from <paramref name="from"/> to <paramref name="to"/>, 0 = up.</summary>
    private static float AngleTo(Vector2 from, Vector2 to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
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
