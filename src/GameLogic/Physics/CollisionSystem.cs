using System;
using System.Numerics;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Physics;

public static class CollisionSystem
{
    public static bool BulletHitsTank(BulletEntity bullet, TankEntity tank)
    {
        if (!bullet.IsAlive || !tank.IsAlive || bullet.OwnerId == tank.Id)
            return false;

        float dist = Vector2.Distance(bullet.Position, tank.Position);
        return dist <= Constants.BulletRadius + Constants.TankRadius;
    }

    public static bool BulletHitsWall(BulletEntity bullet, WallData wall)
    {
        if (!bullet.IsAlive) return false;

        float bx = bullet.Position.X;
        float by = bullet.Position.Y;
        float r = Constants.BulletRadius;

        return bx + r > wall.X && bx - r < wall.Right
            && by + r > wall.Y && by - r < wall.Bottom;
    }

    public static bool IsOutOfBounds(BulletEntity bullet)
    {
        return bullet.Position.X < 0 || bullet.Position.X > Constants.MapWidth
            || bullet.Position.Y < 0 || bullet.Position.Y > Constants.MapHeight;
    }

    public static bool TanksOverlap(TankEntity a, TankEntity b)
    {
        float dist = Vector2.Distance(a.Position, b.Position);
        return dist < Constants.TankRadius * 2f;
    }

    /// <summary>
    /// Pushes the tank out of a wall if overlapping. Returns true if a collision occurred.
    /// </summary>
    public static bool ResolveTankWallCollision(TankEntity tank, WallData wall)
    {
        float tx = tank.Position.X;
        float ty = tank.Position.Y;
        float r = Constants.TankRadius;

        float left = wall.X - r;
        float right = wall.Right + r;
        float top = wall.Y - r;
        float bottom = wall.Bottom + r;

        if (tx < left || tx > right || ty < top || ty > bottom)
            return false;

        // Find shallowest overlap axis and push out
        float overlapLeft = tx - left;
        float overlapRight = right - tx;
        float overlapTop = ty - top;
        float overlapBottom = bottom - ty;

        float minOverlap = MathF.Min(MathF.Min(overlapLeft, overlapRight),
                                     MathF.Min(overlapTop, overlapBottom));

        Vector2 resolved = tank.Position;
        if (minOverlap == overlapLeft) resolved.X = left;
        else if (minOverlap == overlapRight) resolved.X = right;
        else if (minOverlap == overlapTop) resolved.Y = top;
        else resolved.Y = bottom;

        tank.SetPosition(resolved);
        return true;
    }

    /// <summary>Returns true when the segment from <paramref name="from"/> to <paramref name="to"/> is not blocked by any wall.</summary>
    public static bool HasLineOfSight(Vector2 from, Vector2 to, WallData[] walls)
    {
        foreach (var wall in walls)
        {
            if (SegmentIntersectsRect(from, to, wall))
                return false;
        }
        return true;
    }

    private static bool SegmentIntersectsRect(Vector2 a, Vector2 b, WallData wall)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;

        float txMin, txMax, tyMin, tyMax;

        if (dx != 0)
        {
            txMin = (wall.X - a.X) / dx;
            txMax = (wall.Right - a.X) / dx;
            if (txMin > txMax) (txMin, txMax) = (txMax, txMin);
        }
        else
        {
            if (a.X < wall.X || a.X > wall.Right) return false;
            txMin = float.NegativeInfinity;
            txMax = float.PositiveInfinity;
        }

        if (dy != 0)
        {
            tyMin = (wall.Y - a.Y) / dy;
            tyMax = (wall.Bottom - a.Y) / dy;
            if (tyMin > tyMax) (tyMin, tyMax) = (tyMax, tyMin);
        }
        else
        {
            if (a.Y < wall.Y || a.Y > wall.Bottom) return false;
            tyMin = float.NegativeInfinity;
            tyMax = float.PositiveInfinity;
        }

        float tMin = MathF.Max(txMin, tyMin);
        float tMax = MathF.Min(txMax, tyMax);

        return tMin <= tMax && tMax >= 0f && tMin <= 1f;
    }

    /// <summary>Clamps tank inside map bounds.</summary>
    public static void ClampTankToMap(TankEntity tank)
    {
        float r = Constants.TankRadius;
        var pos = tank.Position;
        pos.X = Math.Clamp(pos.X, r, Constants.MapWidth - r);
        pos.Y = Math.Clamp(pos.Y, r, Constants.MapHeight - r);
        tank.SetPosition(pos);
    }
}
